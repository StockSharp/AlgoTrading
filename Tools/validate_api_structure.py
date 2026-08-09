#!/usr/bin/env python3
"""Validate strategy translations and C#/Python implementation parity."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path


REQUIRED_READMES = (
    "README.md",
    "README_ru.md",
    "README_zh.md",
    "README_es.md",
    "README_de.md",
    "README_pt.md",
    "README_ja.md",
)

STRATEGY_NAME = re.compile(r"^\d{4}_.+")
RANGE_NAME = re.compile(r"^\d{4}-\d{4}$")
STALE_PYTHON_CLAIM = re.compile(
    r"(?:"
    r"\b(?:no|not|without|omit(?:ted|s|ting)?|absent|missing|unavailable|"
    r"later|yet|future|avoid(?:s|ed|ing)?|never)\b.{0,120}\bpython\b"
    r"|"
    r"\bpython\b.{0,120}\b(?:no|not|without|omit(?:ted|s|ting)?|absent|"
    r"missing|unavailable|later|yet|future|avoid(?:s|ed|ing)?|never)\b"
    r"|"
    r"\bonly\b.{0,80}\bc#\b"
    r")",
    re.IGNORECASE,
)


def run_git(repo: Path, *args: str, nul_separated: bool = False) -> list[str]:
    command = [
        "git",
        "-C",
        str(repo),
        "-c",
        "core.quotepath=false",
        *args,
    ]
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    if result.returncode != 0:
        message = result.stderr.decode("utf-8", errors="replace").strip()
        raise RuntimeError(message or f"Git exited with code {result.returncode}.")

    separator = b"\0" if nul_separated else None
    chunks = result.stdout.split(separator) if separator else result.stdout.splitlines()
    return [chunk.decode("utf-8", errors="surrogateescape") for chunk in chunks if chunk]


def find_repo_root(api_root: Path) -> Path:
    lines = run_git(api_root, "rev-parse", "--show-toplevel")

    if not lines:
        raise RuntimeError(f"Unable to find the Git repository containing: {api_root}")

    return Path(lines[0]).resolve()


def parse_args() -> argparse.Namespace:
    default_api = Path(__file__).resolve().parents[1] / "API"
    parser = argparse.ArgumentParser(
        description="Check every API strategy for all translations and both implementations."
    )
    parser.add_argument(
        "--api",
        type=Path,
        default=default_api,
        help=f"API directory to validate (default: {default_api})",
    )
    return parser.parse_args()


def find_stale_python_claims(text: str) -> list[tuple[int, str]]:
    claims: list[tuple[int, str]] = []

    for line_number, line in enumerate(text.splitlines(), start=1):
        for sentence in re.split(r"(?<=[.!?])\s+", line):
            if "python" not in sentence.casefold():
                continue

            if STALE_PYTHON_CLAIM.search(sentence):
                claims.append((line_number, sentence.strip()))

    return claims


def validate_readme_encoding(
    api_root: Path, readme_relative: str
) -> tuple[str, str | None, str | None]:
    try:
        text = (api_root / Path(readme_relative)).read_text(encoding="utf-8-sig")
    except UnicodeDecodeError as error:
        return (
            readme_relative,
            None,
            f"{readme_relative}: invalid UTF-8 at byte {error.start}",
        )

    issue = None

    if "\ufffd" in text:
        issue = f"{readme_relative}: contains the Unicode replacement character"

    english_text = text if readme_relative.endswith("/README.md") else None
    return readme_relative, english_text, issue


def main() -> int:
    args = parse_args()
    api_root = args.api.resolve()

    if not api_root.is_dir():
        print(f"API directory not found: {api_root}", file=sys.stderr)
        return 1

    try:
        repo_root = find_repo_root(api_root)
        api_relative = api_root.relative_to(repo_root).as_posix()
        indexed_files = run_git(
            repo_root,
            "ls-files",
            "-z",
            "--cached",
            "--others",
            "--exclude-standard",
            "--",
            api_relative,
            nul_separated=True,
        )
        deleted_files = set(
            run_git(
                repo_root,
                "ls-files",
                "-z",
                "--deleted",
                "--",
                api_relative,
                nul_separated=True,
            )
        )
    except (RuntimeError, ValueError) as error:
        print(error, file=sys.stderr)
        return 1

    api_prefix = f"{api_relative.rstrip('/')}/"
    files: set[str] = set()
    strategies: set[str] = set()
    csharp_strategies: set[str] = set()
    python_strategies: set[str] = set()
    nested_readmes: set[str] = set()

    for repo_file in indexed_files:
        repo_file = repo_file.replace("\\", "/")

        if repo_file in deleted_files or not repo_file.startswith(api_prefix):
            continue

        relative_file = repo_file[len(api_prefix) :]

        if not relative_file:
            continue

        files.add(relative_file)
        parts = relative_file.split("/")

        if STRATEGY_NAME.fullmatch(parts[0]):
            strategy_index = 0
        elif (
            len(parts) >= 2
            and RANGE_NAME.fullmatch(parts[0])
            and STRATEGY_NAME.fullmatch(parts[1])
        ):
            strategy_index = 1
        else:
            continue

        strategy = "/".join(parts[: strategy_index + 1])
        strategies.add(strategy)

        if len(parts) <= strategy_index + 2:
            continue

        if any(part.casefold() in {"bin", "obj"} for part in parts[strategy_index + 2 : -1]):
            continue

        implementation = parts[strategy_index + 1]
        filename = parts[-1]

        if (
            implementation in {"CS", "PY"}
            and filename.casefold().startswith("readme")
            and filename.casefold().endswith(".md")
        ):
            nested_readmes.add(relative_file)

        if implementation == "CS" and filename.endswith(".cs"):
            csharp_strategies.add(strategy)
        elif implementation == "PY" and filename.endswith(".py"):
            python_strategies.add(strategy)

    if not strategies:
        print(f"No strategy directories were found under: {api_root}", file=sys.stderr)
        return 1

    issues: list[str] = []

    for nested_readme in sorted(nested_readmes):
        issues.append(
            f"{nested_readme}: implementation-specific README is not allowed; "
            "keep documentation in the strategy root"
        )

    readmes_to_validate = [
        f"{strategy}/{readme_name}"
        for strategy in sorted(strategies)
        for readme_name in REQUIRED_READMES
        if f"{strategy}/{readme_name}" in files
    ]
    english_texts: dict[str, str] = {}

    with ThreadPoolExecutor() as executor:
        results = executor.map(
            lambda readme: validate_readme_encoding(api_root, readme),
            readmes_to_validate,
        )

        for readme_relative, english_text, encoding_issue in results:
            if encoding_issue is not None:
                issues.append(encoding_issue)

            if english_text is not None:
                english_texts[readme_relative.rsplit("/", 1)[0]] = english_text

    for strategy in sorted(strategies):
        for readme_name in REQUIRED_READMES:
            readme_relative = f"{strategy}/{readme_name}"

            if readme_relative not in files:
                issues.append(f"{strategy}: missing {readme_name}")

        if strategy not in csharp_strategies:
            issues.append(
                f"{strategy}: missing C# implementation (expected a .cs file under CS/)"
            )

        if strategy not in python_strategies:
            issues.append(
                f"{strategy}: missing Python implementation (expected a .py file under PY/)"
            )

        if (
            strategy in english_texts
            and strategy in csharp_strategies
            and strategy in python_strategies
        ):
            english_readme = f"{strategy}/README.md"

            for line_number, claim in find_stale_python_claims(english_texts[strategy]):
                issues.append(
                    f"{english_readme}:{line_number}: stale implementation claim: "
                    f"{claim[:160]}"
                )

    if issues:
        print(f"API structure validation failed with {len(issues)} issue(s):", file=sys.stderr)

        for issue in issues:
            print(f"  - {issue}", file=sys.stderr)

        return 1

    readme_count = len(strategies) * len(REQUIRED_READMES)
    print(
        "API structure validation passed: "
        f"{len(strategies)} strategies, {readme_count} README files, "
        "C#/Python parity confirmed."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
