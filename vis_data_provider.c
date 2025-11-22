#include <pulse/pulseaudio.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include "visualizer.h"
#include <kissfft/kiss_fft.h>
#include <kissfft/kiss_fftr.h>

static pa_mainloop* mainloop = NULL;
static pa_context* context = NULL;
static pa_stream* stream = NULL;

#define e   2.718
#define pi  3.141

#define SAMPLE_COUNT 1024 * 2
#define NFFT SAMPLE_COUNT

typedef struct {
    float real;
    float imag;
} complex;


static char default_sink_name[1024] = {0};
static visu_t* vis = NULL;
static int32_t max_magnitude = 1;
static int32_t used_max_magnitude = 10000;
static uint32_t frame_c = 0;

void connect_audio_stream();    
void context_state_callback(pa_context* c, void* userdata);


void fast_fourier_2(const int16_t* samples, size_t sample_count) {
    int num_bars = vis->count;
    if (num_bars <= 0) return;

    float in[NFFT];
    for (int i = 0; i < NFFT; i++) {
        if ((size_t)i < sample_count) in[i] = (float)samples[i];
        else in[i] = 0.0f;
    }

    kiss_fft_cpx out[NFFT / 2 + 1];
    static kiss_fftr_cfg cfg = NULL;
    if (!cfg) cfg = kiss_fftr_alloc(NFFT, 0, NULL, NULL);

    kiss_fftr(cfg, in, out);

    float mags[NFFT / 2];
    
    // Magnitudes + Pre-weighting
    for (int k = 0; k < NFFT / 2; k++) {
        float re = out[k].r;
        float im = out[k].i;
        float mag = sqrtf(re * re + im * im);
        
        // Multiplies high freq by up to 20x to make them visible
        float boost = 1.0f + ((float)k / ((float) NFFT / 2)) * 20.0f; 
        mags[k] = mag * boost; 
    }

    // Logarithmic Mapping with Interpolation
    float min_idx = 1.0f;
    float max_idx = (float)((float) NFFT / 2) - 2.0f;
    float log_base = max_idx / min_idx;

    for (int i = 0; i < num_bars; i++) {
        float ratio = (float)i / (float)num_bars;
        float idx = min_idx * powf(log_base, ratio);
        
        int idx_i = (int)idx;
        float frac = idx - (float)idx_i; // 0-1

        // Linear Interpolation
        float val1 = mags[idx_i];
        float val2 = mags[idx_i + 1];
        float interpolated_value = val1 * (1.0f - frac) + val2 * frac;

        vis->height[i] = interpolated_value; 
    }

    // Auto-scale
    float cur_max_mag = 1e-6f;
    for (int i = 0; i < num_bars; i++) {
        if (vis->height[i] > cur_max_mag) {
			cur_max_mag = vis->height[i];
		}
    }

	if (cur_max_mag > max_magnitude) {
        printf("[%d] update magnitude scaling\n", frame_c);
        max_magnitude = cur_max_mag;
        used_max_magnitude = cur_max_mag;
    }
	// slow sensitivity raising
    max_magnitude += (cur_max_mag - max_magnitude) * 0.1f;
    

    if (++frame_c % 500 == 0) {
        used_max_magnitude = max_magnitude;
        printf("[%d] update magnitude scaling\n", frame_c);
    }

    // output mapping
    for (int i = 0; i < num_bars; i++) {
        float x = vis->height[i] / (used_max_magnitude * 1.05f);
        if (x > 1.0f) x = 1.0f;
        if (x < 0.0f) x = 0.0f;
        vis->height[i] = x;
    }
}


// threaded code
void* vis_write_data(void* vis_ptr) {
	vis = (visu_t*) vis_ptr;

	printf("Visualizer: %d\n", vis->count);
    mainloop = pa_mainloop_new();
    context = pa_context_new(pa_mainloop_get_api(mainloop), "Visualizer");
    pa_context_set_state_callback(context, context_state_callback, NULL);
    pa_context_connect(context, NULL, PA_CONTEXT_NOFLAGS, NULL);

    int ret;
    if (pa_mainloop_run(mainloop, &ret) < 0) {
        fprintf(stderr, "mainloop  failed\n");
        exit(1);
    }

    pa_stream_disconnect(stream);
    pa_stream_unref(stream);
    pa_context_disconnect(context);
    pa_context_unref(context);
    pa_mainloop_free(mainloop);

	return NULL;
}


void stream_read_callback(pa_stream* s, size_t length, void* userdata) {
    const void* data;
	(void) userdata; // do not need
    if (pa_stream_peek(s, &data, &length) < 0) return;

    if (length > 0) {
        const int16_t* samples = (const int16_t*) data;
        size_t sample_count = length / sizeof(int16_t);
		fast_fourier_2(samples, sample_count);
	}

    pa_stream_drop(s);
}

void stream_state_callback(pa_stream* s, void* userdata) {
	(void) userdata; // do not need
    switch (pa_stream_get_state(s)) {
        case PA_STREAM_READY:
            printf("Stream ready\n");
            break;
        case PA_STREAM_FAILED:
        case PA_STREAM_TERMINATED:
            pa_mainloop_quit(mainloop, 0);
            break;
        default:
            break;
    }
}

static void server_info_cb(pa_context *c, const pa_server_info *info, void *userdata) {
	(void) c;
	(void) userdata;
    snprintf(default_sink_name, 1024, "%s", info->default_sink_name);
    printf("Default Sink Name Retrieved: %s\n", info->default_sink_name);
    connect_audio_stream(); 
}

static void sink_info_list_cb(pa_context *c, const pa_sink_info *info, int eol, void *userdata) {
	(void) userdata;
	(void) c;
    if (eol > 0) {
        pa_operation *o = pa_context_get_server_info(context, server_info_cb, NULL);
        pa_operation_unref(o);
        return;
    }
    if (info) {
        printf("Sink Name: %s | %s\n", info->name, info->description);
    }
}

void connect_audio_stream() {
    printf("Default Sink found: %s. Connecting stream...\n", default_sink_name);
    const char* source_name = default_sink_name;
    
    // Check if the stream is already connected or if failed
    if (!context || default_sink_name[0] == 0) return;

    pa_sample_spec ss = {
        .format = PA_SAMPLE_S16LE,
        .rate = 44100,
        .channels = 2
    };

    stream = pa_stream_new(context, "VisualizerStream", &ss, NULL);
    pa_stream_set_read_callback(stream, stream_read_callback, NULL);
    pa_stream_set_state_callback(stream, stream_state_callback, NULL);
    
    pa_buffer_attr buffer_attr;
    buffer_attr.maxlength = (uint32_t)-1;
    buffer_attr.tlength = (uint32_t)-1;
    buffer_attr.prebuf  = (uint32_t)-1;
    buffer_attr.minreq  = (uint32_t)-1;
    buffer_attr.fragsize = SAMPLE_COUNT * sizeof(int16_t);

    char monitor_source_name[1024 + 10];
    snprintf(monitor_source_name, 1024 + 10, "%s.monitor", source_name);

    pa_stream_connect_record(stream, monitor_source_name, &buffer_attr, PA_STREAM_ADJUST_LATENCY);
    // if monitor name fails fallback to NULL for default monitor source
    if (pa_stream_get_state(stream) == PA_STREAM_TERMINATED) {
        printf("Failed to connect to monitor. Falling back to default source (NULL).\n");
        pa_stream_connect_record(stream, NULL, &buffer_attr, PA_STREAM_ADJUST_LATENCY);
    }
}

void context_state_callback(pa_context* c, void* userdata) {
	(void) userdata;
	switch (pa_context_get_state(c)) {
		case PA_CONTEXT_READY:
			printf("Requesting device info...\n");
			pa_operation *o = pa_context_get_sink_info_list(context, sink_info_list_cb, NULL);
            pa_operation_unref(o);
			break;

		case PA_CONTEXT_FAILED:
		case PA_CONTEXT_TERMINATED:
			pa_mainloop_quit(mainloop, 0);
			break;
		default:
			break;
	}
}

