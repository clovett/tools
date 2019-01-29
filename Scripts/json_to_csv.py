import sys
import json

def load_json(filename):
    with open(filename,"r") as f:
        return json.load(f)

if len(sys.argv) != 2:
    print("Usage: json_to_csv jsonfile")
    sys.exit(1)

data = load_json(sys.argv[1])

columns = []

for x in data:
    for c in x:
        if not c in columns:
            columns += [c]

header = ""
for c in columns:
    if header != "":
        header += ","
    header += str(c)

print(header)

for x in data:
    row = ""
    for c in columns:
        if row != "":
            row += ","
        if c in x:
            row += str(x[c])

    print(row)