
#ifndef __VISUALIZER_H__
#define __VISUALIZER_H__
#define MAX_BARS 1024

typedef struct visu {
	int count;
	int x[MAX_BARS];
	int y[MAX_BARS];
	int width;
	volatile float height[MAX_BARS];
} visu_t;


void visu_init(visu_t* vis, int bar_width, int gap);
void visu_render(visu_t* vis);

#endif
