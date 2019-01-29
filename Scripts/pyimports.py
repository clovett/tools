import os
import re
import glob
from pathlib import Path

def find_imports(file):
    s = open(file,"r").readlines()
    result = []
    for x in [ line.strip() for line in s if re.match("^from|^import", line)]:
        if re.match("from[\s]+__future__[\s]+import", x):
            # special case
            x = x[x.index("import"):]
        
        m = re.match("from[\s]+([^\s]+)[\s]+import", x)
        if m:
            result += [m.group(1)]
        else:
            m = re.match("import[\s]+([^\s]+)", x)
            if m:
                result += [m.group(1)]
        
    return result
    
def find_all_imports(root):
    basenames = []
    imports = []
    for file in list(Path(root).glob('**/*.py')):
        print("searching {}...".format(file))
        bn = os.path.splitext(os.path.basename(file))[0]
        if bn not in basenames:
            basenames += [bn]
        for i in find_imports(file):
            if i not in imports:
                imports += [i]
        
    externals = []
    for i in imports:
        if i not in basenames and i not in externals:
            externals += [i]

    externals.sort()
    return externals

[print(x) for x in find_all_imports("./")]