#pragma once

namespace Generator
{
    public ref class RandomGenerator sealed
    {
    private:
        struct RandomGeneratorImpl;
        std::unique_ptr<RandomGeneratorImpl> _impl;
    public:
        RandomGenerator(int count);

        double GetNext();
    };
}
