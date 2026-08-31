#!/usr/bin/env python3
"""Validate strategy translations and C#/Python implementation parity."""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from collections import defaultdict
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

CACHE_VERSION = 1
CACHE_SIGNATURE = (
    f"{CACHE_VERSION}:{STALE_PYTHON_CLAIM.flags}:{STALE_PYTHON_CLAIM.pattern}"
)
DEFAULT_WORKERS = min(256, max(32, (os.cpu_count() or 1) * 8))


def iter_api_files(api_root: Path) -> list[str]:
    files: list[str] = []

    for root, directories, filenames in os.walk(api_root):
        directories[:] = [
            directory
            for directory in directories
            if directory.casefold() not in {"bin", "obj"}
        ]
        root_path = Path(root)

        for filename in filenames:
            files.append((root_path / filename).relative_to(api_root).as_posix())

    return files


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
    parser.add_argument(
        "--workers",
        type=int,
        default=DEFAULT_WORKERS,
        help=f"README validation workers (default: {DEFAULT_WORKERS})",
    )
    parser.add_argument(
        "--no-cache",
        action="store_true",
        help="Read every README instead of reusing content-addressed Git-index results.",
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
) -> tuple[str, list[tuple[int, str]], str | None]:
    try:
        text = (api_root / Path(readme_relative)).read_text(encoding="utf-8-sig")
    except UnicodeDecodeError as error:
        return (
            readme_relative,
            [],
            f"{readme_relative}: invalid UTF-8 at byte {error.start}",
        )

    issue = None

    if "\ufffd" in text:
        issue = f"{readme_relative}: contains the Unicode replacement character"

    stale_claims = (
        find_stale_python_claims(text)
        if readme_relative.endswith("/README.md")
        else []
    )
    return readme_relative, stale_claims, issue


def get_git_cache_context(
    api_root: Path, readmes: set[str]
) -> tuple[Path | None, dict[str, str]]:
    """Return an ignored cache path and index blob IDs safe for reuse.

    A cached result is used only for an ordinary stage-0 tracked file which
    ``git diff-files`` confirms is unchanged in the working tree. Modified,
    untracked, assume-unchanged, skip-worktree, and unmerged files are always
    read from disk.
    """

    try:
        repo_result = subprocess.run(
            ["git", "rev-parse", "--show-toplevel"],
            cwd=api_root,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            timeout=30,
        )
        repo_root = Path(os.fsdecode(repo_result.stdout).strip()).resolve()
        api_relative = api_root.relative_to(repo_root).as_posix()

        changed_result = subprocess.run(
            [
                "git",
                "diff-files",
                "--no-ext-diff",
                "--name-only",
                "-z",
                "--",
                api_relative,
            ],
            cwd=repo_root,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            timeout=30,
        )
        changed = {
            os.fsdecode(path).replace("\\", "/")
            for path in changed_result.stdout.split(b"\0")
            if path
        }

        index_result = subprocess.run(
            ["git", "ls-files", "-s", "-v", "-z", "--", api_relative],
            cwd=repo_root,
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            timeout=30,
        )
    except (
        OSError,
        subprocess.CalledProcessError,
        subprocess.TimeoutExpired,
        ValueError,
    ):
        return None, {}

    prefix = f"{api_relative.rstrip('/')}/"
    blob_ids: dict[str, str] = {}

    for raw_entry in index_result.stdout.split(b"\0"):
        if not raw_entry or b"\t" not in raw_entry:
            continue

        metadata, raw_path = raw_entry.split(b"\t", 1)
        metadata_parts = metadata.split()

        if len(metadata_parts) != 4:
            continue

        tag, _, raw_blob_id, stage = metadata_parts
        repo_path = os.fsdecode(raw_path).replace("\\", "/")

        # H is a normal tracked entry. Lower-case tags denote assume-unchanged;
        # S denotes skip-worktree. Neither is safe for a working-tree cache hit.
        if tag != b"H" or stage != b"0" or repo_path in changed:
            continue

        if not repo_path.startswith(prefix):
            continue

        readme_relative = repo_path[len(prefix) :]

        if readme_relative in readmes:
            blob_ids[readme_relative] = raw_blob_id.decode("ascii")

    cache_path = repo_root / "obj" / "validate_api_structure" / "readmes-v1.json"
    return cache_path, blob_ids


def load_readme_cache(cache_path: Path | None) -> dict[str, dict[str, object]]:
    if cache_path is None:
        return {}

    try:
        payload = json.loads(cache_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError):
        return {}

    if payload.get("signature") != CACHE_SIGNATURE:
        return {}

    entries = payload.get("entries")
    return entries if isinstance(entries, dict) else {}


def read_cached_result(
    readme_relative: str, entry: object
) -> tuple[str, list[tuple[int, str]], str | None] | None:
    if not isinstance(entry, dict):
        return None

    issue = entry.get("issue")
    raw_claims = entry.get("stale_claims")

    if issue is not None and not isinstance(issue, str):
        return None

    if not isinstance(raw_claims, list):
        return None

    claims: list[tuple[int, str]] = []

    for raw_claim in raw_claims:
        if (
            not isinstance(raw_claim, list)
            or len(raw_claim) != 2
            or not isinstance(raw_claim[0], int)
            or not isinstance(raw_claim[1], str)
        ):
            return None

        claims.append((raw_claim[0], raw_claim[1]))

    return readme_relative, claims, issue


def save_readme_cache(
    cache_path: Path | None, entries: dict[str, dict[str, object]]
) -> None:
    if cache_path is None:
        return

    temporary_path = cache_path.with_name(f"{cache_path.name}.{os.getpid()}.tmp")

    try:
        cache_path.parent.mkdir(parents=True, exist_ok=True)
        temporary_path.write_text(
            json.dumps(
                {"signature": CACHE_SIGNATURE, "entries": entries},
                ensure_ascii=False,
                separators=(",", ":"),
            ),
            encoding="utf-8",
        )
        os.replace(temporary_path, cache_path)
    except OSError:
        try:
            temporary_path.unlink(missing_ok=True)
        except OSError:
            pass


def main() -> int:
    args = parse_args()
    api_root = args.api.resolve()

    if args.workers < 1:
        print("--workers must be at least 1", file=sys.stderr)
        return 1

    if not api_root.is_dir():
        print(f"API directory not found: {api_root}", file=sys.stderr)
        return 1

    indexed_files = iter_api_files(api_root)
    files: set[str] = set()
    strategies: set[str] = set()
    csharp_strategies: set[str] = set()
    python_strategies: set[str] = set()
    csharp_files: dict[str, list[str]] = defaultdict(list)
    python_files: dict[str, list[str]] = defaultdict(list)
    nested_readmes: set[str] = set()

    for repo_file in indexed_files:
        repo_file = repo_file.replace("\\", "/")
        relative_file = repo_file

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
            csharp_files[strategy].append(relative_file)
        elif implementation == "PY" and filename.endswith(".py"):
            python_strategies.add(strategy)
            python_files[strategy].append(relative_file)

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
    readme_set = set(readmes_to_validate)

    if args.no_cache:
        cache_path, blob_ids = None, {}
    else:
        cache_path, blob_ids = get_git_cache_context(api_root, readme_set)

    cached_entries = load_readme_cache(cache_path)
    retained_entries: dict[str, dict[str, object]] = {}
    cache_needs_save = False
    results_by_readme: dict[
        str, tuple[str, list[tuple[int, str]], str | None]
    ] = {}
    readmes_to_read: list[str] = []

    for readme_relative in readmes_to_validate:
        blob_id = blob_ids.get(readme_relative)
        cache_key = f"{readme_relative}:{blob_id}" if blob_id is not None else None
        cached_result = (
            read_cached_result(readme_relative, cached_entries.get(cache_key))
            if cache_key is not None
            else None
        )

        if cached_result is None:
            readmes_to_read.append(readme_relative)
            continue

        results_by_readme[readme_relative] = cached_result
        retained_entries[cache_key] = cached_entries[cache_key]

    with ThreadPoolExecutor(max_workers=args.workers) as executor:
        results = executor.map(
            lambda readme: validate_readme_encoding(api_root, readme),
            readmes_to_read,
        )

        for readme_relative, stale_claims, encoding_issue in results:
            result = (readme_relative, stale_claims, encoding_issue)
            results_by_readme[readme_relative] = result

            blob_id = blob_ids.get(readme_relative)

            if blob_id is not None:
                cache_key = f"{readme_relative}:{blob_id}"
                cache_entry = {
                    "stale_claims": stale_claims,
                    "issue": encoding_issue,
                }
                retained_entries[cache_key] = cache_entry
                cache_needs_save = (
                    cache_needs_save or cached_entries.get(cache_key) != cache_entry
                )

    cache_needs_save = cache_needs_save or len(retained_entries) != len(cached_entries)

    if not args.no_cache and cache_needs_save:
        save_readme_cache(cache_path, retained_entries)

    english_claims: dict[str, list[tuple[int, str]]] = {}

    for readme_relative in readmes_to_validate:
        _, stale_claims, encoding_issue = results_by_readme[readme_relative]

        if encoding_issue is not None:
            issues.append(encoding_issue)

        if readme_relative.endswith("/README.md"):
            english_claims[readme_relative.rsplit("/", 1)[0]] = stale_claims

    for strategy in sorted(strategies):
        for readme_name in REQUIRED_READMES:
            readme_relative = f"{strategy}/{readme_name}"

            if readme_relative not in files:
                issues.append(f"{strategy}: missing {readme_name}")

        if strategy not in csharp_strategies:
            issues.append(
                f"{strategy}: missing C# implementation (expected a .cs file under CS/)"
            )
        elif len(csharp_files[strategy]) != 1:
            issues.append(
                f"{strategy}: expected exactly one C# implementation, found "
                f"{len(csharp_files[strategy])}: {', '.join(sorted(csharp_files[strategy]))}"
            )

        if strategy not in python_strategies:
            issues.append(
                f"{strategy}: missing Python implementation (expected a .py file under PY/)"
            )
        elif len(python_files[strategy]) != 1:
            issues.append(
                f"{strategy}: expected exactly one Python implementation, found "
                f"{len(python_files[strategy])}: {', '.join(sorted(python_files[strategy]))}"
            )

        if (
            strategy in english_claims
            and strategy in csharp_strategies
            and strategy in python_strategies
        ):
            english_readme = f"{strategy}/README.md"

            for line_number, claim in english_claims[strategy]:
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
