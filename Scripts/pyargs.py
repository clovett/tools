import sys

args = sys.argv
args.pop(0)

print("\"args\": [")
for a in args:
  print("    \"{}\",".format(a.replace("\\","\\\\")))
print("]")