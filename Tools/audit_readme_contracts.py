#!/usr/bin/env python3
"""Reproducible README-contract triage over every strategy in API.

This is deliberately a candidate finder, not a semantic validator. Missing parameter
names/defaults can be aliases or delegated settings; identical trading bodies can be intentional.
Absent explicit native stop calls can mean manual/delegated protection, not a proven defect.
Every finding still needs README review and an independent behavioral test in both languages.
The tool is read-only and never marks a strategy as accepted.
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import re
import sys
import tokenize
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor
from decimal import Decimal, DecimalException
from pathlib import Path


NATIVE_PARAMETERS = {"Security", "Portfolio", "Volume", "Name", "LogLevel"}
DEFAULT_BULLET = re.compile(r"^\s*[-*]\s+`([A-Za-z]\w*)\s*(?:=|`\s*[:=])", re.MULTILINE)
CS_PARAMETER = re.compile(r'\bParam(?:<[^>\n]+>)?\s*\(\s*(?:nameof\s*\(\s*(\w+)\s*\)|"(\w+)")')
PY_PARAMETER = re.compile(r'\bself\.Param\s*\(\s*[\'"](\w+)[\'"]')
CS_LEXEME = re.compile(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|//[^\n]*|/\*[\s\S]*?\*/')
CS_METHOD = re.compile(
    r"\b(?:private|protected|public|internal)\s+(?:[\w<>\[\],?.]+\s+)*"
    r"(?P<name>\w+)\s*\([^{};]*\)\s*\{"
)
TRADING_CALL = re.compile(r"\b(?:BuyMarket|SellMarket|RegisterOrder|RegisterStopOrder|ClosePosition)\s*\(")
NUMBER_LITERAL = re.compile(r"[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?[mMdDfF]?")
STOP_PROMISE = re.compile(r"^\s*[-*]\s*(?:\*\*)?Stops(?:\*\*)?\s*:\s*Yes\b", re.MULTILINE | re.IGNORECASE)
NATIVE_STOP_SETUP = re.compile(r"\b(?:StartProtection|RegisterStopOrder)\s*\(")


def cs_without_comments(source: str, mask_strings: bool = False) -> str:
    def replace(match: re.Match[str]) -> str:
        value = match.group()
        if value.startswith(("//", "/*")) or mask_strings:
            return re.sub(r"[^\n]", " ", value)
        return value

    return CS_LEXEME.sub(replace, source)


def py_without_comments(source: str, mask_strings: bool = False) -> str:
    lines = source.splitlines(keepends=True)
    try:
        for token in tokenize.generate_tokens(io.StringIO(source).readline):
            if token.type == tokenize.COMMENT or mask_strings and token.type == tokenize.STRING:
                for row in range(token.start[0], token.end[0] + 1):
                    line = lines[row - 1]
                    start = token.start[1] if row == token.start[0] else 0
                    end = token.end[1] if row == token.end[0] else len(line)
                    lines[row - 1] = line[:start] + re.sub(r"[^\r\n]", " ", line[start:end]) + line[end:]
    except (tokenize.TokenError, IndentationError):
        # Triage may encounter incomplete examples. Preserve the text, but do not claim syntax validity.
        pass
    return "".join(lines)


def documented_default_expressions(readme: str) -> dict[str, str]:
    expressions = {match[1]: readme[match.end():].partition("\n")[0].strip().strip("`")
                   for match in DEFAULT_BULLET.finditer(readme)}
    default_column = None
    for line in readme.splitlines():
        if not line.strip().startswith("|"):
            default_column = None
            continue
        columns = [column.strip() for column in line.strip().strip("|").split("|")]
        if any("default" in column.casefold() for column in columns):
            default_column = next(i for i, column in enumerate(columns) if "default" in column.casefold())
            continue
        if default_column is not None and len(columns) > default_column and columns[default_column]:
            match = re.fullmatch(r"`([A-Za-z]\w*)`", columns[0])
            if match:
                expressions[match[1]] = columns[default_column].strip("`")
    return {name: value for name, value in expressions.items() if name not in NATIVE_PARAMETERS}


def documented_defaults(readme: str) -> set[str]:
    return set(documented_default_expressions(readme))


def decimal_literal_text(number: Decimal) -> str | None:
    # Avoid context rounding and unbounded expansion of hostile scientific literals.
    if not number.is_finite() or abs(number.as_tuple().exponent) > 4096 or len(number.as_tuple().digits) > 4096:
        return None
    if number == 0:
        return "0"
    value = format(number, "f")
    return value.rstrip("0").rstrip(".") if "." in value else value


def literal_default(expression: str | None) -> str | None:
    """Normalize only supported literals; never evaluate code or infer opaque expressions."""
    if expression is None:
        return None
    expression = expression.strip().strip("`")
    if len(expression) > 256:
        return None
    if expression.casefold() in ("true", "false"):
        return "bool:" + expression.casefold()
    if NUMBER_LITERAL.fullmatch(expression):
        try:
            value = decimal_literal_text(Decimal(expression.rstrip("mMdDfF")))
        except DecimalException:
            return None
        return "number:" + value if value is not None else None
    wrapper = "DataType.TimeFrame("
    if expression.startswith(wrapper) and expression.endswith(")"):
        expression = expression[len(wrapper):-1].strip()
    if expression.endswith(".TimeFrame()"):
        expression = expression[:-len(".TimeFrame()")].strip()
    time = re.fullmatch(r"TimeSpan\.From(Milliseconds|Seconds|Minutes|Hours|Days)\(\s*([^()]*)\s*\)", expression)
    if time and NUMBER_LITERAL.fullmatch(time[2].strip()):
        try:
            amount = Decimal(time[2].strip().rstrip("mMdDfF"))
            if decimal_literal_text(amount) is None:
                return None
            seconds = amount * {
                "Milliseconds": Decimal("0.001"), "Seconds": Decimal(1), "Minutes": Decimal(60),
                "Hours": Decimal(3600), "Days": Decimal(86400),
            }[time[1]]
            value = decimal_literal_text(seconds)
        except DecimalException:
            return None
        return "duration_seconds:" + value if value is not None else None
    return None


def second_argument(source: str, position: int) -> str | None:
    """Read one balanced argument after the already matched parameter name."""
    while position < len(source) and source[position].isspace():
        position += 1
    if position == len(source) or source[position] != ",":
        return None
    start = position + 1
    depth = 0
    for position in range(start, len(source)):
        char = source[position]
        # String defaults are deliberately opaque; parentheses inside them are not code.
        if char in "\"'":
            return None
        if char == "," and depth == 0 or char == ")" and depth == 0:
            return source[start:position].strip()
        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
    return None


def parameter_defaults(source: str, pattern: re.Pattern, masked: str, csharp: bool) -> dict[str, str | None]:
    values = defaultdict(set)
    for match in pattern.finditer(source):
        if masked[match.start()] != source[match.start()]:
            continue
        name = (match[1] or match[2]) if csharp else match[1]
        if name not in NATIVE_PARAMETERS:
            values[name].add(literal_default(second_argument(source, match.end())))
    # Multiple alternative declarations are not an excuse to guess the runtime default.
    return {name: next(iter(options)) if len(options) == 1 else None for name, options in values.items()}


def audit_text(key: str, readme: str, csharp: str, python: str) -> dict:
    expressions = documented_default_expressions(readme)
    declared = set(expressions)
    cs_masked = cs_without_comments(csharp, mask_strings=True)
    py_masked = py_without_comments(python, mask_strings=True)
    cs_values = parameter_defaults(cs_without_comments(csharp), CS_PARAMETER, cs_masked, True)
    py_values = parameter_defaults(py_without_comments(python), PY_PARAMETER, py_masked, False)
    cs, py = set(cs_values), set(py_values)
    mismatches = []
    for name, expression in sorted(expressions.items()):
        documented = literal_default(expression)
        if documented is None:
            # Permit a prose annotation after a numeric README literal, not source arithmetic.
            annotation = re.fullmatch(r"(" + NUMBER_LITERAL.pattern + r")\s+\([^()]*\)", expression)
            if annotation:
                documented = literal_default(annotation[1])
        if documented is None:
            continue
        languages = [language for language, values in (("cs", cs_values), ("py", py_values))
                     if values.get(name) is not None and values[name] != documented]
        if languages:
            mismatches.append({"parameter": name, "documented": documented,
                               "cs_default": cs_values.get(name), "py_default": py_values.get(name),
                               "languages": languages})
    # This is an explicit-native-installation heuristic. Manual/delegated stops still need review;
    # presence of a call is not proof that a stop executes, and absence is not proof of no protection.
    stops = [language for language, source in (("cs", cs_masked), ("py", py_masked))
             if STOP_PROMISE.search(readme) and not NATIVE_STOP_SETUP.search(source)]
    return {
        "key": key,
        "status": "unreviewed_candidate",
        "documented_defaults": sorted(declared),
        "missing_cs": sorted(declared - cs),
        "missing_py": sorted(declared - py),
        "cs_only_parameters": sorted(cs - py),
        "py_only_parameters": sorted(py - cs),
        "default_mismatches": mismatches,
        "stop_promise_without_native_setup": stops,
    }


def trading_bodies(source: str) -> list[tuple[str, str]]:
    masked = cs_without_comments(source, mask_strings=True)
    cleaned = cs_without_comments(source)
    bodies = []
    for match in CS_METHOD.finditer(masked):
        start = match.end() - 1
        depth = 1
        end = start + 1
        while end < len(masked) and depth:
            depth += (masked[end] == "{") - (masked[end] == "}")
            end += 1
        if depth or not TRADING_CALL.search(masked[start:end]):
            continue
        text = cleaned[start:end]
        # Formatting outside literals is irrelevant. Spaces inside a string are data.
        parts = []
        previous = 0
        for literal in CS_LEXEME.finditer(text):
            parts.append(re.sub(r"\s+", "", text[previous:literal.start()]))
            parts.append(literal.group())
            previous = literal.end()
        parts.append(re.sub(r"\s+", "", text[previous:]))
        body = "".join(parts)
        bodies.append((match["name"], body))
    return bodies


def clone_families(sources: dict[str, str], minimum: int = 5) -> list[dict]:
    groups = defaultdict(list)
    for key, source in sorted(sources.items()):
        for method, body in trading_bodies(source):
            groups[body].append((key, method))
    result = []
    for body, members in groups.items():
        keys = sorted({key for key, _ in members})
        if len(keys) < minimum:
            continue
        result.append({
            "fingerprint": hashlib.sha256(body.encode("utf-8")).hexdigest()[:16],
            "status": "unreviewed_candidate",
            "keys": keys,
            "methods": sorted({method for _, method in members}),
        })
    return sorted(result, key=lambda family: (-len(family["keys"]), family["fingerprint"]))


def scan(api: Path, minimum: int, workers: int = 16, progress: bool = False, keys: set[str] | None = None) -> dict:
    sources = {}
    parameter_findings = []
    default_value_findings = []
    stop_promise_findings = []
    incomplete = []
    strategies = []
    for range_path in sorted(api.iterdir()):
        if not range_path.is_dir() or not re.fullmatch(r"\d{4}-\d{4}", range_path.name):
            continue
        for strategy in sorted(range_path.iterdir()):
            if not strategy.is_dir() or not re.fullmatch(r"\d{4}_.+", strategy.name):
                continue
            if keys is not None and strategy.name not in keys:
                continue
            strategies.append(strategy)
    if keys is not None:
        missing = keys - {strategy.name for strategy in strategies}
        if missing:
            raise ValueError("Unknown strategy key(s): " + ", ".join(sorted(missing)))

    def load(strategy: Path):
        cs_files = sorted((strategy / "CS").glob("*.cs"))
        py_files = sorted((strategy / "PY").glob("*.py"))
        readme = strategy / "README.md"
        if len(cs_files) != 1 or len(py_files) != 1 or not readme.is_file():
            return strategy.name, None
        return strategy.name, (readme.read_text(encoding="utf-8-sig"),
                               cs_files[0].read_text(encoding="utf-8-sig"),
                               py_files[0].read_text(encoding="utf-8-sig"))

    with ThreadPoolExecutor(max_workers=workers) as pool:
        for index, (key, text) in enumerate(pool.map(load, strategies), start=1):
            if text is None:
                incomplete.append(key)
                continue
            sources[key] = text[1]
            finding = audit_text(key, *text)
            if any(finding[name] for name in ("missing_cs", "missing_py", "cs_only_parameters", "py_only_parameters")):
                parameter_findings.append(finding)
            if finding["default_mismatches"]:
                default_value_findings.append(finding)
            if finding["stop_promise_without_native_setup"]:
                stop_promise_findings.append(finding)
            if progress and (index % 250 == 0 or index == len(strategies)):
                print(f"Audited {index}/{len(strategies)} strategy folders", file=sys.stderr, flush=True)
    families = clone_families(sources, minimum)
    clone_keys = {key for family in families for key in family["keys"]}
    parameter_keys = {finding["key"] for finding in parameter_findings}
    default_keys = {finding["key"] for finding in default_value_findings}
    stop_keys = {finding["key"] for finding in stop_promise_findings}
    return {
        "scope": ("Selected" if keys is not None else "All") + " paired API implementations and English READMEs; heuristic triage only, no semantic acceptance.",
        "summary": {
            "scanned_strategies": len(sources),
            "incomplete_strategy_folders": len(incomplete),
            "parameter_candidates": len(parameter_keys),
            "default_value_candidates": len(default_keys),
            "stop_promise_candidates": len(stop_keys),
            "shared_body_families": len(families),
            "shared_body_candidates": len(clone_keys),
            "unique_candidates": len(parameter_keys | clone_keys | default_keys | stop_keys),
        },
        "incomplete": incomplete,
        "parameter_findings": parameter_findings,
        "default_value_findings": default_value_findings,
        "stop_promise_findings": stop_promise_findings,
        "shared_body_families": families,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--api", type=Path, default=Path(__file__).resolve().parents[1] / "API")
    parser.add_argument("--clone-min", type=int, default=5, help="Minimum distinct strategies sharing a trading body.")
    parser.add_argument("--json", action="store_true", help="Print the entire deterministic candidate ledger as JSON.")
    parser.add_argument("--workers", type=int, default=16, help="Concurrent source reads (default: 16).")
    parser.add_argument("--progress", action="store_true", help="Report progress to stderr without changing JSON output.")
    parser.add_argument("--key", action="append", help="Limit reads to this exact full strategy key; repeat for a focused audit.")
    args = parser.parse_args()
    if args.clone_min < 2:
        parser.error("--clone-min must be at least 2")
    if args.workers < 1:
        parser.error("--workers must be positive")
    try:
        report = scan(args.api, args.clone_min, args.workers, args.progress, set(args.key) if args.key else None)
    except ValueError as error:
        parser.error(str(error))
    print(json.dumps(report if args.json else {"scope": report["scope"], **report["summary"]}, indent=2))


if __name__ == "__main__":
    main()
