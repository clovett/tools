import argparse
import os
import subprocess


def is_code(file_type: str) -> bool:
    if file_type == ".py" or file_type == ".cs" or file_type == ".cpp" or file_type == ".h":
        return True
    if file_type == ".cmd" or file_type == ".sh" or file_type == ".ps1" or file_type == ".psm1":
        return True
    return False


def add_date_range(cmd: str, after: str, before: str) -> str:
    if after:
        cmd += f" --after {after}"
    if before:
        cmd += f" --before {before}"
    return cmd


def get_authors(after: str, before: str):
    cmd = add_date_range("git shortlog -s -n  --no-merges", after, before)
    result = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    for line in result.stdout.split("\n"):
        line = line.strip()
        i = line.find("\t")
        if i > 0:
            count = line[0:i]
            author = line[i + 1:]
            yield count, author


def git_log(after: str, before: str, author: str):
    cmd = add_date_range(f"git log --oneline --author \"{author}\"", after, before)
    result = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    for line in result.stdout.split("\n"):
        i = line.find(" ")
        if i > 0:
            commit_id = line[0:i]
            commit_message = line[i + 1:]
            yield commit_id, commit_message


def get_commit_size(commit_id: str):
    result = subprocess.run(f"git show {commit_id} --no-notes --stat --oneline", capture_output=True, text=True, encoding="utf-8")
    total = 0
    for line in result.stdout.split("\n"):
        parts = line.split("|")
        file_type = os.path.splitext(parts[0].strip().replace("}", ""))[-1]
        if len(parts) > 1 and is_code(file_type):
            changes = parts[1].strip()
            if changes.startswith("Bin"):
                change_count = 1  # binary files only count as 1 change (e.g. png images)
            else:
                lines_changed = changes.split(" ")[0]
                change_count = int(lines_changed)
            total += change_count
    return total


def main():
    """ Parse command line with --date argument """
    parser = argparse.ArgumentParser(description="Summarize git commits")
    parser.add_argument("--after", help="The date to start summarizing")
    parser.add_argument("--before", help="The date to end summarizing")
    args = parser.parse_args()
    for count, author in get_authors(args.after, args.before):
        tally = 0
        for commit_id, _ in git_log(args.after, args.before, author):
            change_count = get_commit_size(commit_id)
            tally += change_count
        print(f"{count},{tally},{author}")


if __name__ == "__main__":
    main()
