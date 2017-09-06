#include "pch.h"
#include "Generator.h"
#include <random>
#include <string>
#include <vector>

using namespace Generator;
using namespace Platform;

struct RandomGenerator::RandomGeneratorImpl
{
    int seed;
    std::default_random_engine engine;
    //std::uniform_real_distribution<double> dist;
    std::normal_distribution<double> dist;
public:

    void Initialize(int seed) {
        this->seed = seed;
        engine = std::default_random_engine(seed);
    }
    double GetNext() {
        return dist(engine);
    }
};

RandomGenerator::RandomGenerator(int seed)
    : _impl(std::make_unique<RandomGeneratorImpl>())
{
    _impl->Initialize(seed);
}

double RandomGenerator::GetNext()
{
    return _impl->GetNext();
}
