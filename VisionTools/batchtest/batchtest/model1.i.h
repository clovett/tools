//
// ELL SWIG header for module model1
//

#pragma once

#ifndef SWIG
//
// ELL header for module model1
//

#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif
//
// Types
//

struct TensorShape
{
    int32_t rows;
    int32_t columns;
    int32_t channels;
};


//
// Functions
//

// Input size: 150528
// Output size: 1000
void model1_predict(float*, float*);

int32_t model1_GetInputSize();

int32_t model1_GetOutputSize();

int32_t model1_GetNumNodes();

void model1_GetInputShape(int32_t, struct TensorShape*);

void model1_GetOutputShape(int32_t, struct TensorShape*);

#ifdef __cplusplus
} // extern "C"
#endif

#endif // SWIG

void model1_predict(const std::vector<float>& input, std::vector<float>& output);

#ifndef SWIG
void model1_predict(const std::vector<float>& input, std::vector<float>& output)
{
    model1_predict(const_cast<float*>(&input[0]), &output[0]);
}
#endif // SWIG

