#!/usr/bin/env python3
####################################################################################################
##
##  Project:  Embedded Learning Library (ELL)
##  File:     known_hosts.py
##  Authors:  Chris Lovett
##
##  Requires: Python 3.x
##
####################################################################################################

import argparse 
import os
from pathlib import Path
import re
import sys

def load_known_hosts():
    home = Path.home()
    filename = os.path.join(home, ".ssh", "known_hosts")
    with open(filename, "r") as f:
        return f.readlines()

def save_known_hosts(lines):
    home = Path.home()
    filename = os.path.join(home, ".ssh", "known_hosts")
    with open(filename, "w") as f:
        f.writelines(lines)

def remove(expr):
    lines = load_known_hosts()
    i = len(lines) - 1
    found = False
    while i >= 0:
        line = lines[i]
        if re.match(expr, line):
            del lines[i]
            found = True
            print("removing: {}".format(line))
        i = i - 1
    if found:
        save_known_hosts(lines)

def main():
    parser = argparse.ArgumentParser("""Provides some handy edit operations for your known_hosts file""")
    parser.add_argument("--remove", "-r", help="Remove addresses matching given regex")
    args = parser.parse_args()
    if args.remove:
        remove(args.remove)
        
if __name__ == '__main__':
    main()