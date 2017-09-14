// darknet.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

extern "C" {
    extern void run_classifier(int argc, char **argv);
}

int main(int argc, char**argv)
{
    run_classifier(argc, argv);
    return 0;
}

