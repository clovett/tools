#include "stdafx.h"
#include "BatchTest.h"
#include "windows.h"

extern "C" {
#include "image.h"
}
#include "model1.i.h"

double what_time_is_it_now()
{
#ifdef LINUX
	struct timespec now;
	clock_gettime(CLOCK_REALTIME, &now);
	return now.tv_sec + now.tv_nsec*1e-9;
#endif
#ifdef _WIN32
	LARGE_INTEGER lp, freq;
	QueryPerformanceCounter(&lp);
	QueryPerformanceFrequency(&freq);
	return (double)lp.QuadPart / (double)freq.QuadPart;
#endif
}

BatchTest::BatchTest()
{
}


BatchTest::~BatchTest()
{
}

int BatchTest::Run(std::string val_map, std::string labelfile, std::string truthlabelfile, int top)
{
	auto map = load_map(val_map);
	this->labels = load_labels(labelfile);
	this->truthlabels = load_labels(truthlabelfile);
	this->top = top;

	double start = what_time_is_it_now();
	int count = 0;
	int passed = 0;
	for (auto ptr = map.begin(), end = map.end(); ptr != end; ptr++)
	{
		count++;
		entry e = *ptr;
		if (Test(e)) {
			passed++;
			std::cout << "  Test passed (" << count << "), current pass rate " << get_rate(count, passed) << "%" << std::endl;
		}
		else {
			std::cout << "  Test failed (" << count << "), current pass rate " << get_rate(count, passed) << "%" << std::endl;
			std::cout << "  ===> Expecting: " << truthlabels[e.prediction] << std::endl;
		}
	}

	std::cout << "========================================================================" << std::endl;
	std::cout << " Test pass rate: " << get_rate(count, passed) << std::endl;

	double end = what_time_is_it_now();
	std::cout << "Total run time is " << get_time_span(end - start) << std::endl;

	return passed == 0 ? 0 : -1;
}

void top_k(float *a, int n, int k, int *index)
{
	int i, j;
	for (j = 0; j < k; ++j) index[j] = -1;
	for (i = 0; i < n; ++i) {
		int curr = i;
		for (j = 0; j < k; ++j) {
			if ((index[j] < 0) || a[curr] > a[index[j]]) {
				int swap = curr;
				curr = index[j];
				index[j] = swap;
			}
		}
	}
}

void saveData(char* filename, float* buffer, long count)
{
	FILE* ptr = fopen(filename, "wb");
	fwrite(buffer, sizeof(float), count, ptr);
	fclose(ptr);

}

void convertToFloat(char* filename)
{
	FILE* ptr = fopen(filename, "rb");
	fseek(ptr, 0, SEEK_END);
	long size = ftell(ptr);
	long input_size = size / sizeof(double);
	fseek(ptr, 0, SEEK_SET);
	double* raw = (double*)calloc(input_size, sizeof(double));
	fread(raw, sizeof(double), input_size, ptr);
	fclose(ptr);

	float* fraw = (float*)calloc(input_size, sizeof(float));
	for (size_t i = 0; i < input_size; i++)
	{
		fraw[i] = (float)raw[i];
	}
	
	saveData(filename, fraw, input_size);

	free(raw);
	free(fraw);

}

bool BatchTest::Test(entry& e)
{
	//convertToFloat("d:\\temp\\ILSVRC2012_val_00000001.JPEG.dat");

	struct TensorShape shape;
	model1_GetInputShape(0, &shape);
	int input_size = model1_GetInputSize();
	image im = load_image_cntk(e.name.c_str());
	//image im = load_image_color(e.name.c_str(),0,0);
	image r = letterbox_image(im, shape.columns, shape.rows);

	saveData("d:\\temp\\test.dat", r.data, input_size);


	int output_size = model1_GetOutputSize();
	float* predictions = (float*)calloc(output_size, sizeof(float));
	int *indexes = (int*)calloc(top, sizeof(int));
	double start = what_time_is_it_now();

	model1_predict(r.data, &predictions[0]);

	top_k(&predictions[0], output_size, top, indexes);
	double end = what_time_is_it_now();
	std::cout << e.name << ": Predicted in " << end - start << "seconds.\n";
	bool passed = false;
	for (int i = 0; i < top; ++i) {
		int index = indexes[i];
		printf("[%f] %5.2f%%: %s\n", predictions[index], predictions[index] * 100, labels[index].c_str());
		if (labels_match(labels[index], truthlabels[e.prediction])) {
			passed = true;
		}
	}
	if (r.data != im.data) free_image(r);
	free_image(im);
	free(predictions);
	free(indexes);
	return passed;
}