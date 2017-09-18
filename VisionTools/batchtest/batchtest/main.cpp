// batchtest.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "BatchTest.h"
#include <algorithm>
#include <cstdarg>
#include <string>

void print_usage() {
	printf("Usage: batchtest options\n");
	printf("--labels filename			The labels for the predictions from the compiled model\n");
	printf("--truthlabels filename		The labels for the truth file\n");
	printf("--truth filename			The truth map .tsv file mapping image file to expected prediction\n");
	printf("--top number				How many of the top predictions to look at (default 1)\n");
}

static std::string ToLowercase(const std::string& s)
{
	std::string lower = s;
	std::transform(lower.begin(), lower.end(), lower.begin(), ::tolower);
	return lower;
}

int main(int argc, char** argv)
{
	// std::string val_map, std::string labels, std::string truthlabels, int top
	std::string labels;
	std::string truthmap;
	std::string truthlabels;
	int top = 1;

	for (int i = 1; i < argc; i++)
	{
		char* arg = argv[i];
		if (arg[0] == '-') {
			std::string next = (i + 1 < argc) ? argv[++i] : "";
			std::string lower = ToLowercase(arg);
			if (lower == "--labels") {
				labels = next;
			}
			else if (lower == "--truth") {
				truthmap = next;
			}
			else if (lower == "--truthlabels") {
				truthlabels = next;
			}
			else if (lower == "--top") {
				top = std::stoi(next);
			}
			else {
				printf("Unexpected argument: %s\n", arg);
				print_usage();
				return 1;
			}
		}
		else 
		{
			printf("Unexpected option: %s\n", arg);
			print_usage();
			return 1;
		}
	}

	if (labels == "") {
		printf("Missing --labels argument\n");
		print_usage();
		return 1;
	}
	if (truthmap == "") {
		printf("Missing --truth argument\n");
		print_usage();
		return 1;
	}
	if (truthlabels == "") {
		truthlabels = labels;
	}

	BatchTest test;
	return test.Run(truthmap, labels, truthlabels, top);
}

