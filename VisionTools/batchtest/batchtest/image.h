#ifndef IMAGE_H
#define IMAGE_H

#include <stdlib.h>
#include <stdio.h>
#include <float.h>
#include <string.h>
#include <math.h>
#include "box.h"

typedef struct {
	int w;
	int h;
	int c;
	float *data;
} image;

typedef struct {
	int w;
	int h;
	float scale;
	float rad;
	float dx;
	float dy;
	float aspect;
} augment_args;


#ifndef __cplusplus
#ifdef OPENCV
int fill_image_from_stream(CvCapture *cap, image im);
image ipl_to_image(IplImage* src);
void ipl_into_image(IplImage* src, image im);
void flush_stream_buffer(CvCapture *cap, int n);
void show_image_cv(image p, const char *name, IplImage *disp);
#endif
#endif

image load_image(const char *filename, int w, int h, int c);
image load_image_color(const char *filename, int w, int h);
image load_image_cntk(const char *filename);
image make_image(int w, int h, int c);
image resize_image(image im, int w, int h);
image letterbox_image(image im, int w, int h);
image crop_image(image im, int dx, int dy, int w, int h);
image resize_min(image im, int min);
image threshold_image(image im, float thresh);
image mask_to_rgb(image mask);
void free_image(image m);
image copy_image(image p);
image float_to_image(int w, int h, int c, float *data);
void save_image_png(image im, const char *name);
void save_image(image p, const char *name);

float get_color(int c, int x, int max);
void draw_box(image a, int x1, int y1, int x2, int y2, float r, float g, float b);
void draw_bbox(image a, box bbox, int w, float r, float g, float b);
void draw_label(image a, int r, int c, image label, const float *rgb);
void write_label(image a, int r, int c, image *characters, char *string, float *rgb);
image image_distance(image a, image b);
void scale_image(image m, float s);
image rotate_crop_image(image im, float rad, float s, int w, int h, float dx, float dy, float aspect);
image center_crop_image(image im, int w, int h);
image random_crop_image(image im, int w, int h);
image random_augment_image(image im, float angle, float aspect, int low, int high, int w, int h);
augment_args random_augment_args(image im, float angle, float aspect, int low, int high, int w, int h);
void letterbox_image_into(image im, int w, int h, image boxed);
image resize_max(image im, int max);
void translate_image(image m, float s);
void transpose_image(image im);
void embed_image(image source, image dest, int dx, int dy);
void place_image(image im, int w, int h, int dx, int dy, image canvas);
void saturate_image(image im, float sat);
void exposure_image(image im, float sat);
void distort_image(image im, float hue, float sat, float val);
void saturate_exposure_image(image im, float sat, float exposure);
void rgb_to_hsv(image im);
void hsv_to_rgb(image im);
void yuv_to_rgb(image im);
void rgb_to_yuv(image im);


image collapse_image_layers(image source, int border);
image collapse_images_horz(image *ims, int n);
image collapse_images_vert(image *ims, int n);

void show_image_normalized(image im, const char *name);
void show_images(image *ims, int n, char *window);
void show_image_layers(image p, char *name);
void show_image_collapsed(image p, char *name);

void print_image(image m);

image make_empty_image(int w, int h, int c);
void copy_image_into(image src, image dest);

float get_pixel(image m, int x, int y, int c);
float get_pixel_extend(image m, int x, int y, int c);
void set_pixel(image m, int x, int y, int c, float val);
void add_pixel(image m, int x, int y, int c, float val);
float bilinear_interpolate(image im, float x, float y, int c);

image get_image_layer(image m, int l);

#endif

