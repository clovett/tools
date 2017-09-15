// darknet.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <vector>
#include <string>
#include <fstream>
#include <memory.h>
#include <iostream>
#include <algorithm>
#include <locale>
#include <cctype>

extern "C" {
#include "darknet.h"
extern void run_classifier(int argc, char **argv);
}

struct entry {
    std::string name;
    int prediction;
};

std::string get_directory_name(std::string filename)
{
    size_t pos = filename.find_last_of('\\');
    if (pos != std::string::npos) {
        return filename.substr(0, pos);
    }
    return filename;
}

std::vector<entry> load_map(char* filename)
{
    std::cout << "loading " << filename << "...";
    std::string folder = get_directory_name(filename);
    std::vector<entry> map;
    std::ifstream stream(filename);
    while (!stream.eof()) {
        std::string line;
        std::getline(stream, line);
        size_t pos = line.find_first_of('\t');
        if (pos != std::string::npos) {
            std::string name = folder + "\\" + line.substr(0, pos);
            pos++;
            int index = std::stoi(line.substr(pos));
            map.push_back(entry{ name,index });
        }
    }
    std::cout << "found " << map.size() << " test entries" << std::endl;
    return map;
}

// trim from start (in place)
static inline void ltrim(std::string &s) {
    s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](int ch) {
        return !std::isspace(ch);
    }));
}

// trim from end (in place)
static inline void rtrim(std::string &s) {
    s.erase(std::find_if(s.rbegin(), s.rend(), [](int ch) {
        return !std::isspace(ch);
    }).base(), s.end());
}

// trim from both ends (in place)
static inline void trim(std::string &s) {
    ltrim(s);
    rtrim(s);
}

// This is a fuzzy match based on embedded comma separated names provided in the label file.
// If any of the sub-names match then we consider this a match.
bool labels_match(std::string label1, std::string label2)
{
    std::transform(label1.begin(), label1.end(), label1.begin(), ::tolower);
    std::transform(label2.begin(), label2.end(), label2.begin(), ::tolower);
    if (label1 == label2) {
        return true;
    }
    size_t pos1 = 0;
    size_t len1 = label1.size();
    size_t len2 = label2.size();
    while (pos1 < len1) {
        size_t comma = label1.find_first_of(',', pos1);
        std::string part1;
        if (comma == std::string::npos) {
            part1 = label1.substr(pos1);
            pos1 = len1;
        }
        else {
            part1 = label1.substr(pos1, comma - pos1);
            pos1 = comma + 1;
        }
        trim(part1);
        size_t pos2 = 0;
        while (pos2 < len2) {
            comma = label2.find_first_of(',', pos2);
            std::string part2;
            if (comma == std::string::npos) {
                part2 = label2.substr(pos2);
                pos2 = len2;
            }
            else {
                part2 = label2.substr(pos2, comma - pos2);
                pos2 = comma + 1;
            }
            trim(part2);
            if (part1 == part2) {
                return true;
            }
        }
    }
    return false;
}

std::string get_time_span(double seconds) {

    int hours = (int)(seconds / 3600);
    double remainder = seconds - (hours * 3600);
    int minutes = (int)(remainder / 60);
    int secs = (remainder - (60 * minutes));
    char buffer[100];
    sprintf(buffer, "%02d:%02d:%02d", hours, minutes, secs);
    return std::string(buffer);
}

bool test(network& net, char **labels, char** truthlabels, entry& e, int top)
{
    image im = load_image_color(e.name.c_str(), 0, 0);
    image r = letterbox_image(im, net.w, net.h);

    int *indexes = (int*)calloc(top, sizeof(int));
    float *X = r.data;
    double start = what_time_is_it_now();
    float *predictions = network_predict(net, X);
    if (net.hierarchy) hierarchy_predictions(predictions, net.outputs, net.hierarchy, 1, 1);
    top_k(predictions, net.outputs, top, indexes);
    double end = what_time_is_it_now();
    std::cout << e.name << ": Predicted in " << end - start << "seconds.\n";
    bool passed = false;
    for (int i = 0; i < top; ++i) {
        int index = indexes[i];
        printf("[%f] %5.2f%%: %s\n", predictions[index], predictions[index] * 100, labels[index]);
        if (labels_match(labels[index], truthlabels[e.prediction])) {
            passed = true;
        }
    }
    if (r.data != im.data) free_image(r);
    free_image(im);
    free(indexes);
    return passed;
}

int get_rate(int count, int passed)
{
    return (int)(((double)passed * 100.0) / (double)count);
}

int main(int argc, char**argv)
{
    if (argc < 6) {
        std::cout << "usage: darknet cfg_file labels weights_file val_map truth_labels\n";
        return 1;
    }

    char *cfgfile = argv[1];
    char* labelsfile = argv[2];
    char* weightfile = argv[3];
    char* mapfile = argv[4];
    char* truthlabelsfile = argv[5];
    int top = 1;

    network net = parse_network_cfg(cfgfile);
    load_weights(&net, weightfile);
    set_batch_network(&net, 1);
    char **labels = get_labels(labelsfile);
    char **truthlabels = get_labels(truthlabelsfile);
    std::vector<entry> map = load_map(mapfile);

    double start = what_time_is_it_now();
    int count = 0;
    int passed = 0;
    for (auto ptr = map.begin(), end = map.end(); ptr != end; ptr++)
    {
        count++;
        entry e = *ptr;
        bool passed = test(net, labels, truthlabels, e, top);
        if (passed) {
            passed++;
            std::cout << "Test passed (" << count << "), current pass rate " << get_rate(count, passed) << "%" << std::endl;
        }
        else {
            std::cout << "Test failed (" << count << "), current pass rate " << get_rate(count, passed) << "%" << std::endl;
            std::cout << "   ===> Expecting: " << truthlabels[e.prediction] << std::endl;
        }
    }

    std::cout << "========================================================================" << std::endl;
    std::cout << " Test pass rate: " << get_rate(count, passed) << std::endl;

    double end = what_time_is_it_now();
    std::cout << "Total run time is " << get_time_span(end - start) << std::endl;
    return 0;
}

