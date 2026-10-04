"""Tests for the validator's check of StockSharp.BusinessEntities references."""

import unittest

from Tools.validate_api_structure import find_unreferenced_business_entities_imports


HEADER = (
    "import clr\n\n"
    'clr.AddReference("StockSharp.Messages")\n'
    'clr.AddReference("StockSharp.Algo")\n'
)
REFERENCE = 'clr.AddReference("StockSharp.BusinessEntities")\n'


class BusinessEntitiesReferenceTests(unittest.TestCase):
    def test_indicator_value_imported_without_reference(self):
        text = HEADER + "from StockSharp.Algo.Indicators import SimpleMovingAverage, CandleIndicatorValue\n"
        self.assertEqual(["CandleIndicatorValue"], find_unreferenced_business_entities_imports(text))

    def test_reference_satisfies_the_import(self):
        text = HEADER + REFERENCE + "from StockSharp.Algo.Indicators import CandleIndicatorValue, IIndicator\n"
        self.assertEqual([], find_unreferenced_business_entities_imports(text))

    def test_indicators_from_the_algo_assembly_need_no_reference(self):
        text = HEADER + "from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange\n"
        self.assertEqual([], find_unreferenced_business_entities_imports(text))

    def test_parenthesized_and_continued_imports(self):
        parenthesized = HEADER + (
            "from StockSharp.Algo.Indicators import (SimpleMovingAverage,\n"
            "                                        IIndicator)\n"
        )
        continued = HEADER + (
            "from StockSharp.Algo.Indicators import SimpleMovingAverage, \\\n"
            "    IndicatorHelper\n"
        )
        self.assertEqual(["IIndicator"], find_unreferenced_business_entities_imports(parenthesized))
        self.assertEqual(["IndicatorHelper"], find_unreferenced_business_entities_imports(continued))

    def test_import_inside_a_method(self):
        text = HEADER + (
            "class s:\n"
            "    def process(self):\n"
            "        from StockSharp.Algo.Indicators import CandleIndicatorValue\n"
        )
        self.assertEqual(["CandleIndicatorValue"], find_unreferenced_business_entities_imports(text))

    def test_business_entities_namespace_and_wildcard(self):
        text = HEADER + (
            "from StockSharp.BusinessEntities import Security\n"
            "from StockSharp.Algo.Indicators import *\n"
        )
        self.assertEqual(
            ["StockSharp.Algo.Indicators.*", "StockSharp.BusinessEntities"],
            find_unreferenced_business_entities_imports(text),
        )

    def test_commented_reference_does_not_count(self):
        text = HEADER + '# clr.AddReference("StockSharp.BusinessEntities")\n' + (
            "from StockSharp.Algo.Indicators import CandleIndicatorValue\n"
        )
        self.assertEqual(["CandleIndicatorValue"], find_unreferenced_business_entities_imports(text))


if __name__ == "__main__":
    unittest.main()
