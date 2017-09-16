#include <stdio.h>
#include "utils.h"
#include <assert.h>

void predict_classifier(char *datacfg, char *cfgfile, char *weightfile, char *filename, int top)
{
    network net = parse_network_cfg(cfgfile);
    if (weightfile) {
        load_weights(&net, weightfile);
    }
    set_batch_network(&net, 1);
    srand(2222222);

    list *options = read_data_cfg(datacfg);

    char *name_list = option_find_str(options, "names", 0);
    if (!name_list) name_list = option_find_str(options, "labels", "data/labels.list");
    if (top == 0) top = option_find_int(options, "top", 1);

    int i = 0;
    char **names = get_labels(name_list);
    clock_t time;
    int *indexes = calloc(top, sizeof(int));
    char buff[256];
    char *input = buff;
    while (1) {
        if (filename) {
            strncpy(input, filename, 256);
        }
        else {
            printf("Enter Image Path: ");
            fflush(stdout);
            input = fgets(input, 256, stdin);
            if (!input) return;
            strtok(input, "\n");
        }
        image im = load_image_color(input, 0, 0);
        image r = letterbox_image(im, net.w, net.h);
        //resize_network(&net, r.w, r.h);
        //printf("%d %d\n", r.w, r.h);

        float *X = r.data;
        time = clock();
        float *predictions = network_predict(net, X);
        if (net.hierarchy) hierarchy_predictions(predictions, net.outputs, net.hierarchy, 1, 1);
        top_k(predictions, net.outputs, top, indexes);
        fprintf(stderr, "%s: Predicted in %f seconds.\n", input, sec(clock() - time));
        for (i = 0; i < top; ++i) {
            int index = indexes[i];
            printf("[%f] %5.2f%%: %s\n", predictions[index], predictions[index] * 100, names[index]);
        }
        if (r.data != im.data) free_image(r);
        free_image(im);
        if (filename) break;
    }
}


void run_classifier(int argc, char **argv)
{
    if (argc < 4) {
        fprintf(stderr, "usage: %s %s [train/test/valid] [cfg] [weights (optional)]\n", argv[0], argv[1]);
        return;
    }

    int cam_index = find_int_arg(argc, argv, "-c", 0);
    int top = find_int_arg(argc, argv, "-t", 0);
    int clear = find_arg(argc, argv, "-clear");
    char *data = argv[3];
    char *cfg = argv[4];
    char *weights = (argc > 5) ? argv[5] : 0;
    char *filename = (argc > 6) ? argv[6] : 0;
    char *layer_s = (argc > 7) ? argv[7] : 0;
    int layer = layer_s ? atoi(layer_s) : -1;
    if (0 == strcmp(argv[2], "predict")) predict_classifier(data, cfg, weights, filename, top);
}
