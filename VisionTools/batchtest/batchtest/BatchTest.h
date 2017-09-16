#pragma once

#include <vector>
#include <string>
#include <fstream>
#include <memory.h>
#include <iostream>
#include <algorithm>
#include <locale>
#include <cctype>

class BatchTest
{
public:
	BatchTest();
	~BatchTest();

	int Run(std::string val_map, std::string labelfile, std::string truthlabelfile, int top = 1);

private:
	std::vector<std::string> labels;
	std::vector<std::string> truthlabels;
	int top = 1;

	struct entry {
		std::string name;
		int prediction;
	};

	bool Test(entry& e);

	std::string get_directory_name(std::string filename)
	{
		size_t pos = filename.find_last_of('\\');
		if (pos != std::string::npos) {
			return filename.substr(0, pos);
		}
		return filename;
	}

	std::vector<std::string> load_labels(std::string filename)
	{
		std::vector<std::string> list;
		std::ifstream stream(filename);
		while (!stream.eof()) {
			std::string line;
			std::getline(stream, line);
			trim(line);
			if (line.size() > 0) {
				list.push_back(line);
			}
		}
		return list;
	}

	std::vector<entry> load_map(std::string filename)
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
		int secs = (int)(remainder - (60 * minutes));
		char buffer[100];
		sprintf_s(buffer, 100, "%02d:%02d:%02d", hours, minutes, secs);
		return std::string(buffer);
	}

	int get_rate(int count, int passed)
	{
		return (int)(((double)passed * 100.0) / (double)count);
	}

};

