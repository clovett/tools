import argparse
import subprocess


def get_authors(date: str):
    result = subprocess.run(f"git shortlog -s -n  --no-merges --after {date}", capture_output=True, text=True, encoding="utf-8")
    for line in result.stdout.split("\n"):
        line = line.strip()
        i = line.find("\t")
        if i > 0:
            count = line[0:i]
            author = line[i + 1:]
            yield count, author


def git_log(date: str, author: str):
    result = subprocess.run(f"git log --oneline --author \"{author}\" --after {date}", capture_output=True, text=True, encoding="utf-8")
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
        if len(parts) > 1:
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
    parser.add_argument("--date", help="The date to summarize from", required=True)
    args = parser.parse_args()
    for count, author in get_authors(args.date):
        tally = 0
        for commit_id, _ in git_log(args.date, author):
            change_count = get_commit_size(commit_id)
            tally += change_count
        print(f"{count},{tally},{author}")


if __name__ == "__main__":
    main()
