import os
import sys
import json
import csv

def load_csv(filename):  
    data = []  
    with open(filename,"r") as f:
        for row in csv.reader(f, delimiter=','):
            data += [row]
    return data

def save_json(data, filename):
    with open(filename, "w") as f:
        json.dump(data, f, indent=2, sort_keys=True)


if len(sys.argv) != 2:
    print("Usage: csv_to_json csvfile")
    sys.exit(1)

filename = os.path.abspath(sys.argv[1])

data = load_csv(filename)

jsondata = []
first = True
for row in data:
    if first:
        names = row
        first = False
    else:
        d = {}
        for i in range(len(names)):
            d[names[i]] = row[i]
        jsondata += [d]

jsonfile = os.path.join(os.path.dirname(filename), os.path.splitext(os.path.basename(filename))[0] + ".json")
save_json(jsondata, jsonfile)


