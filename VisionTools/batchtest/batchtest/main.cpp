// batchtest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "BatchTest.h"

int main(int argc, char** argv)
{
	// std::string val_map, std::string labels, std::string truthlabels, int top
	if (argc != 5) {
		printf("Usage: batchtest val_map labels truthlabels top");
		printf("e.g. batchtest d:\\temp\\cntk\\testset\val_map.txt darknetImageNetLabels.txt cntkVgg16ImageNetLabels.txt 5");
	}


	int top = atoi(argv[4]);

	BatchTest test;
	return test.Run(argv[1], argv[2], argv[3], top);
}

