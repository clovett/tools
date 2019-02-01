import os
import sys
import shutil

path = os.getenv("CUDA_PATH")
if not path:
    path = input("Please enter your CUDA SDK install path:")

cudnn = input("Please enter path to unzipped cudnn package:")

if not os.path.isdir(cudnn):
    print("path not found: {}".format(cudnn))
    sys.exit(1)

if os.path.isdir(os.path.join(cudnn, "cuda")):
    cudnn = os.path.join(cudnn, "cuda")

includes = os.path.join(cudnn, "include")
if not os.path.isdir(includes):
    print("path not found: {}".format(includes))
    sys.exit(1)

libs = os.path.join(cudnn, "lib","x64")
if not os.path.isdir(libs):
    print("path not found: {}".format(libs))
    sys.exit(1)

bins = os.path.join(cudnn, "bin")
if not os.path.isdir(bins):
    print("path not found: {}".format(bins))
    sys.exit(1)

print("Copying includes...")
for name in os.listdir(includes):
    shutil.copyfile(os.path.join(includes, name), os.path.join(path, "include", name))


print("Copying libraries...")
for name in os.listdir(libs):
    shutil.copyfile(os.path.join(libs, name), os.path.join(path, "lib", "x64", name))


print("Copying dlls...")
for name in os.listdir(bins):
    shutil.copyfile(os.path.join(bins, name), os.path.join(path, "bin", name))
