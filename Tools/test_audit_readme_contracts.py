"""Tests for the audit's candidate detection, not strategy acceptance."""

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from Tools.audit_readme_contracts import audit_text, clone_families, scan, trading_bodies


class ReadmeAuditTests(unittest.TestCase):
    def test_documented_defaults_missing_from_both_implementations(self):
        result = audit_text(
            "0016_RSI_Divergence",
            "- `RsiPeriod` = 14\n- `StopLossPercent` = 2m\n- `Volume` = 1m",
            '_rsi = Param(nameof(RsiPeriod), 14);',
            'self._rsi = self.Param("RsiPeriod", 14)',
        )
        self.assertEqual(["StopLossPercent"], result["missing_cs"])
        self.assertEqual(["StopLossPercent"], result["missing_py"])
        self.assertEqual([], result["cs_only_parameters"])

    def test_parameter_presence_is_checked_in_both_languages(self):
        result = audit_text(
            "1101_Multi_Timeframe_MACD", "",
            'Param<DataType>(nameof(HigherCandleType), type); Param("CandleType", type);',
            'self.Param("CandleType", candle_type)',
        )
        self.assertEqual(["HigherCandleType"], result["cs_only_parameters"])
        self.assertEqual([], result["py_only_parameters"])

    def test_comments_cannot_satisfy_a_documented_parameter(self):
        result = audit_text(
            "0016_RSI_Divergence", "- `StopLossPercent` = 2m",
            '// Param(nameof(StopLossPercent), 2m);',
            '# self.Param("StopLossPercent", 2.0)',
        )
        self.assertEqual(["StopLossPercent"], result["missing_cs"])
        self.assertEqual(["StopLossPercent"], result["missing_py"])

    def test_default_table_is_distinct_from_a_description_table(self):
        result = audit_text(
            "0001_Example",
            "| Name | Default | Description |\n| --- | --- | --- |\n| `Length` | 14 | Period |\n\n"
            "| Parameter | Description |\n| --- | --- |\n| `Unrelated` | Security to use |",
            "", "",
        )
        self.assertEqual(["Length"], result["missing_cs"])

    def test_string_braces_and_comments_do_not_corrupt_method_boundaries(self):
        source = 'private void ProcessCandle() { var label = "}"; /* { */ BuyMarket(1m); }'
        bodies = trading_bodies(source)
        self.assertEqual(1, len(bodies))
        self.assertEqual("ProcessCandle", bodies[0][0])
        self.assertIn('"}"', bodies[0][1])

    def test_clone_matching_preserves_semantically_different_literals(self):
        base = 'private void ProcessCandle() { if (Position == 0) BuyMarket(1m); }'
        formatted = 'private void ProcessCandle() { /* comment */\n if (Position == 0)\n BuyMarket(1m); }'
        different = 'private void ProcessCandle() { if (Position == 0) BuyMarket(2m); }'
        groups = clone_families({"0001_A": base, "0002_B": formatted, "0003_C": different}, minimum=2)
        self.assertEqual(1, len(groups))
        self.assertEqual(["0001_A", "0002_B"], groups[0]["keys"])

    def test_shared_bodies_are_candidates_not_claims_of_contract_failure(self):
        source = 'private void ProcessCandle() { BuyMarket(); }'
        groups = clone_families({"0001_A": source, "0002_B": source}, minimum=2)
        self.assertEqual("unreviewed_candidate", groups[0]["status"])

    def test_clone_matching_preserves_whitespace_inside_string_literals(self):
        spaced = 'private void ProcessCandle() { BuyMarket().Comment = "Grid Buy"; }'
        compact = 'private void ProcessCandle() { BuyMarket().Comment = "GridBuy"; }'
        self.assertEqual([], clone_families({"0001_A": spaced, "0002_B": compact}, minimum=2))

    def test_correct_parameter_names_cannot_hide_wrong_timeframe_defaults(self):
        result = audit_text(
            "0048_VWAP_Breakout", "- `CandleType` = TimeSpan.FromMinutes(5)",
            'Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());',
            'self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))',
        )
        self.assertEqual([], result["missing_cs"])
        self.assertEqual([], result["missing_py"])
        self.assertEqual([{
            "parameter": "CandleType", "documented": "duration_seconds:300",
            "cs_default": "duration_seconds:60", "py_default": "duration_seconds:60",
            "languages": ["cs", "py"],
        }], result["default_mismatches"])

    def test_numeric_suffixes_equivalent_durations_and_table_defaults(self):
        result = audit_text(
            "0001_Example",
            "- `StopLossPercent` = 2m (optional)\n- `CandleType` = TimeSpan.FromHours(1)\n"
            "| Name | Default | Description |\n| --- | --- | --- |\n| `Length` | 14 | Period |",
            'Param(nameof(StopLossPercent), 2.0m); Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame()); Param(nameof(Length), 10);',
            'self.Param("StopLossPercent", 2.0)\nself.Param("CandleType", DataType.TimeFrame(TimeSpan.FromSeconds(3600)))\nself.Param("Length", 14)',
        )
        self.assertEqual([{
            "parameter": "Length", "documented": "number:14", "cs_default": "number:10",
            "py_default": "number:14", "languages": ["cs"],
        }], result["default_mismatches"])

    def test_unknown_default_expressions_are_not_guessed_or_executed(self):
        result = audit_text(
            "0001_Example", "- `Length` = 14",
            'Param(nameof(Length), RuntimePeriod(7, 2));',
            'self.Param("Length", 7 * multiplier)',
        )
        self.assertEqual([], result["default_mismatches"])

    def test_parameter_mentions_inside_strings_cannot_satisfy_a_contract(self):
        result = audit_text(
            "0001_Example", "- `StopLossPercent` = 2m",
            'var description = "Param(nameof(StopLossPercent), 2m)";',
            '\"\"\"self.Param("StopLossPercent", 2.0)\"\"\"',
        )
        self.assertEqual(["StopLossPercent"], result["missing_cs"])
        self.assertEqual(["StopLossPercent"], result["missing_py"])

    def test_stop_promises_cannot_be_satisfied_by_comments_strings_or_lifecycle(self):
        result = audit_text(
            "0050_AD", "- **Stops**: Yes.",
            '/* StartProtection(); */ var label = "RegisterStopOrder()"; protected void OnStopped() { }',
            '# self.StartProtection()\n\"\"\"self.RegisterStopOrder()\"\"\"\ndef OnStopped(self):\n    pass',
        )
        self.assertEqual(["cs", "py"], result["stop_promise_without_native_setup"])
        self.assertEqual("unreviewed_candidate", result["status"])

    def test_explicit_native_stop_calls_and_no_stop_claim_are_not_candidates(self):
        for readme, cs, py in (
            ("- Stops: Yes; optional", 'StartProtection(new Unit(), new Unit(2m));', 'self.StartProtection(Unit(), Unit(2.0))'),
            ("- **Stops**: No.", 'BuyMarket();', 'self.BuyMarket()'),
        ):
            self.assertEqual([], audit_text("0001_Example", readme, cs, py)["stop_promise_without_native_setup"])

    def test_manual_stops_without_native_installation_remain_review_candidates(self):
        result = audit_text(
            "0001_ManualStop", "- Stops: Yes",
            'if (price < _stopPrice) SellMarket(Position);',
            'if price < self._stop_price:\n    self.SellMarket(self.Position)',
        )
        self.assertEqual(["cs", "py"], result["stop_promise_without_native_setup"])
        self.assertEqual("unreviewed_candidate", result["status"])

    def test_decimal_literals_are_compared_without_context_rounding(self):
        result = audit_text(
            "0001_Example", "- `Limit` = 79228162514264337593543950333m",
            'Param(nameof(Limit), 79228162514264337593543950332m);',
            'self.Param("Limit", 79228162514264337593543950333)',
        )
        self.assertEqual(["cs"], result["default_mismatches"][0]["languages"])

    def test_boolean_defaults_are_not_numbers(self):
        result = audit_text("0001_Example", "- `Enabled` = true",
                            'Param(nameof(Enabled), false);', 'self.Param("Enabled", True)')
        self.assertEqual(["cs"], result["default_mismatches"][0]["languages"])

    def test_ambiguous_alternative_defaults_are_not_guessed(self):
        result = audit_text("0001_Example", "- `Length` = 14",
                            'Param(nameof(Length), 14); Param(nameof(Length), 10);',
                            'self.Param("Length", 14)')
        self.assertEqual([], result["default_mismatches"])

    def test_pathological_scientific_defaults_are_opaque_not_expanded(self):
        result = audit_text("0001_Example", "- `Length` = 14\n- `CandleType` = TimeSpan.FromMinutes(5)",
                            'Param(nameof(Length), 1e999999999999999999999m); Param(nameof(CandleType), TimeSpan.FromMinutes(1e999999999));',
                            'self.Param("Length", 1e999999999999999999999)\nself.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1e999999999)))')
        self.assertEqual([], result["default_mismatches"])

    def test_selected_inventory_scope_and_candidate_union(self):
        workspace_obj = Path(__file__).resolve().parents[1] / "obj"
        workspace_obj.mkdir(exist_ok=True)
        with TemporaryDirectory(prefix="readme-audit-test-", dir=workspace_obj) as fixture:
            api = Path(fixture)
            for key, period, stops in (("0001_A", 10, "Yes"), ("0002_B", 14, "No")):
                strategy = api / "0001-0100" / key
                (strategy / "CS").mkdir(parents=True)
                (strategy / "PY").mkdir()
                (strategy / "README.md").write_text(f"- `Length` = 14\n- Stops: {stops}", encoding="utf-8")
                (strategy / "CS" / "Strategy.cs").write_text(
                    f'Param(nameof(Length), {period}); private void ProcessCandle() {{ BuyMarket(); }}', encoding="utf-8")
                (strategy / "PY" / "strategy.py").write_text(f'self.Param("Length", {period})', encoding="utf-8")
            selected = scan(api, minimum=2, workers=1, keys={"0001_A"})
            self.assertTrue(selected["scope"].startswith("Selected"))
            self.assertEqual(1, selected["summary"]["scanned_strategies"])
            self.assertEqual(0, selected["summary"]["parameter_candidates"])
            self.assertEqual(1, selected["summary"]["default_value_candidates"])
            self.assertEqual(1, selected["summary"]["stop_promise_candidates"])
            self.assertEqual(1, selected["summary"]["unique_candidates"])
            complete = scan(api, minimum=2, workers=1)
            self.assertTrue(complete["scope"].startswith("All"))
            self.assertEqual(2, complete["summary"]["scanned_strategies"])
            self.assertEqual(2, complete["summary"]["unique_candidates"])

    def test_unknown_selected_key_cannot_report_a_clean_empty_audit(self):
        workspace_obj = Path(__file__).resolve().parents[1] / "obj"
        workspace_obj.mkdir(exist_ok=True)
        with TemporaryDirectory(prefix="readme-audit-test-", dir=workspace_obj) as fixture:
            with self.assertRaisesRegex(ValueError, "Unknown strategy key"):
                scan(Path(fixture), minimum=2, workers=1, keys={"9999_Typo"})


if __name__ == "__main__":
    unittest.main()
