#include "raylib.h"
#include <endian.h>
#include <string.h>
#include <stdio.h>
#include <pthread.h>
#include <math.h>
#include <stdint.h>
#include "vis_data_provider.h"


#define MAX_BARS 1024
#define cool_down_THRESHHOLD 0.04

typedef struct visu {
	int count;
	int x[MAX_BARS];
	int y[MAX_BARS];
	int width;
	volatile float height[MAX_BARS];
} visu_t;

const int screenWidth = 1600;
const int screenHeight = 900;
static uint32_t render_step = 1;
const uint32_t target_fps = 144;

static visu_t visualizer;
static float last_h[MAX_BARS];
static int cool_down = 0;


void visu_init(visu_t* vis, int bar_width, int gap) {
	// calculate how many bars would fit
	int count = screenWidth / (bar_width + gap) - 1;
	int padding = screenWidth - (count * (bar_width + gap) - gap);
	vis->width = bar_width;
	vis->count = count;
	for (int i = 0; i < count; ++i) {
		vis->x[i] = (padding / 2) + i * (bar_width + gap);
		vis->height[i] = 0.0f;
		vis->y[i] = screenHeight * 0.0f;
	}
}

void visu_render(visu_t* vis) {
	for (int i = 0; i < vis->count; ++i) {
		if (cool_down) {	// ignore smaller changes
			if (!(render_step++ % (target_fps * 100) == 0)) {
				if (fabs(vis->height[i] - last_h[i]) < cool_down_THRESHHOLD) {
					vis->height[i] = last_h[i];
					last_h[i] = vis->height[i];
				}
			} else {
				last_h[i] = 2.f;
				printf("reset\n");
			}

		}

		int bottom = screenHeight - vis->height[i];
		int height = (int) screenHeight - ((float) screenHeight * vis->height[i]);

		if (bottom - height < 2) 
			height = screenHeight - 2;

		float fade = (float) i / vis->count;
		Color c = ColorFromHSV(fade * 80 + 260, 1 - fade, 1 - fade);
		DrawRectangle(vis->x[i], height, vis->width, bottom + 1, c);
	}
}

void print_help(void) {
	printf("Help\n");
	printf("Using default pulseaudio device\n");
	printf("Switch shaders:\t\t\t 0-9\n");
	printf("ToggleFPS: \t\t\t f\n");
	printf("Toggle Filter low changes: \t c\n");
	return;
}

void shader_init_values(Shader* shaders, RenderTexture2D target, int index);

void load_shaders(Shader* shaders, RenderTexture2D target) {
	// 0 index -> shader disabled
	shaders[1] = LoadShader(0, "shaders/blur_2.fs");
	shaders[2] = LoadShader(0, "shaders/blur.fs");
	shaders[3] = LoadShader(0, "shaders/glow.fs");
	shaders[4] = LoadShader(0, "shaders/glow_2.fs");
	shaders[5] = LoadShader(0, "shaders/glow_3.fs");
	shaders[6] = LoadShader(0, "shaders/funky.fs");
	shaders[7] = LoadShader(0, "shaders/interesting.fs");
	shaders[8] = LoadShader(0, "shaders/light.fs");
	shaders[9] = LoadShader(0, "shaders/light.fs");
	for (int i = 1; i < 10; ++i)
		shader_init_values(shaders, target, i);
}

static int bar_w = 10;
static int gap_w = 5;

void shader_init_values(Shader* shaders, RenderTexture2D target, int index) {
    Shader sh = shaders[index];
    float fw = (float) screenWidth;
    float fh = (float) screenHeight;
    int loc;

    loc = GetShaderLocation(sh, "renderWidth");
    if (loc != -1) SetShaderValue(sh, loc, &fw, SHADER_UNIFORM_FLOAT);

    loc = GetShaderLocation(sh, "renderHeight");
    if (loc != -1) SetShaderValue(sh, loc, &fh, SHADER_UNIFORM_FLOAT);

    loc = GetShaderLocation(sh, "lightBrightness");
    if (loc != -1) {
        float lightVal = 1.0f;
        SetShaderValue(sh, loc, &lightVal, SHADER_UNIFORM_FLOAT);
    }

    loc = GetShaderLocation(sh, "barCount");
    if (loc != -1) {
        int barCount = visualizer.count;
        SetShaderValue(sh, loc, &barCount, SHADER_UNIFORM_INT);
    }

    loc = GetShaderLocation(sh, "barWidth");
    if (loc != -1) {
        float barWidth  = (float) bar_w;
        SetShaderValue(sh, loc, &barWidth, SHADER_UNIFORM_FLOAT);
    }

    loc = GetShaderLocation(sh, "barSpacing");
    if (loc != -1) {
        float barSpacing = (float) gap_w;
        SetShaderValue(sh, loc, &barSpacing, SHADER_UNIFORM_FLOAT);
    }

    loc = GetShaderLocation(sh, "barHeights");
    SetTextureWrap(target.texture, TEXTURE_WRAP_CLAMP);
}


int main(int argc, char** argv) {
	int fps_on = 0;
	int shader_num = 1;

    if ((argc > 1) && (strcmp("-h", argv[1]) == 0 || strcmp("--help", argv[1]) == 0)) {
        print_help();
        return 0;
    }

    // Initer
    InitWindow(screenWidth, screenHeight, "Visualizer");
    SetTargetFPS(target_fps);
	// Loading shaders
	Shader shaders[10] = {0};
	// starting with default shader 1
    RenderTexture2D target = LoadRenderTexture(screenWidth, screenHeight);
    visu_init(&visualizer, bar_w, gap_w);

	load_shaders(shaders, target);

    // get audio and write to visualizer
    pthread_t vis_thread;
    pthread_create(&vis_thread, NULL, vis_write_data, (void*)(&visualizer));

    int barHeightsLoc = GetShaderLocation(shaders[shader_num], "barHeights");

    while (!WindowShouldClose()) {
		if (shader_num) {
			SetShaderValue(shaders[shader_num], barHeightsLoc, (const float*) visualizer.height, SHADER_UNIFORM_FLOAT);
		}

        BeginTextureMode(target);
        ClearBackground(BLACK);
        visu_render(&visualizer);
        EndTextureMode();

		// keyboard controls
        if (IsKeyReleased(KEY_F)) {
			fps_on = fps_on ^ 1;
		}
		// filtering of just small changes
		if (IsKeyReleased(KEY_C)) {
			cool_down = cool_down ^ 1;
		}

        BeginDrawing();
        ClearBackground(BLACK);

		if (shader_num) BeginShaderMode(shaders[shader_num]);
		Rectangle source = { 0, 0, (float) target.texture.width, (float) - target.texture.height };
		Rectangle dest   = { 0, 0, (float) screenWidth, (float) screenHeight };
		Vector2 origin   = { 0, 0 };
		DrawTexturePro(target.texture, source, dest, origin, 0.0f, WHITE);
		if (shader_num) EndShaderMode();

		if (fps_on) DrawFPS(20, 40);

		for (int key = KEY_ONE; key <= KEY_NINE; key++) {
			if (IsKeyReleased(key)) {
				int index = key - KEY_ONE;
				printf("Switching shaders: %d\n", index);
				shader_num = index;
				barHeightsLoc = GetShaderLocation(shaders[shader_num], "barHeights");
			}
		}	

		EndDrawing();
	}

	UnloadRenderTexture(target);
	for (int i = 1; i < 10; i++) {
		UnloadShader(shaders[i]);
	}

	CloseWindow();
	return 0;
}


