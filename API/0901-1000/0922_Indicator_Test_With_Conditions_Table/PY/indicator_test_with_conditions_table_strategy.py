import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class indicator_test_with_conditions_table_strategy(Strategy):
    def __init__(self):
        super(indicator_test_with_conditions_table_strategy, self).__init__()
        self._enable_long_cond = self.Param("EnableLongCond", True) \
            .SetDisplay("Enable Long Condition", "Enable long entry condition", "Long Entry")
        self._long_operator = self.Param("LongOperator", ">") \
            .SetDisplay("Long Operator", "Operator for long entry", "Long Entry")
        self._long_value = self.Param("LongValue", 0.0) \
            .SetDisplay("Long Value", "Comparison value for long entry", "Long Entry")
        self._enable_close_long_cond = self.Param("EnableCloseLongCond", False) \
            .SetDisplay("Enable Close Long", "Enable close long condition", "Close Long")
        self._close_long_operator = self.Param("CloseLongOperator", "<") \
            .SetDisplay("Close Long Operator", "Operator for closing long", "Close Long")
        self._close_long_value = self.Param("CloseLongValue", 0.0) \
            .SetDisplay("Close Long Value", "Value for closing long", "Close Long")
        self._enable_short_cond = self.Param("EnableShortCond", False) \
            .SetDisplay("Enable Short Condition", "Enable short entry condition", "Short Entry")
        self._short_operator = self.Param("ShortOperator", "<") \
            .SetDisplay("Short Operator", "Operator for short entry", "Short Entry")
        self._short_value = self.Param("ShortValue", 0.0) \
            .SetDisplay("Short Value", "Comparison value for short entry", "Short Entry")
        self._enable_close_short_cond = self.Param("EnableCloseShortCond", False) \
            .SetDisplay("Enable Close Short", "Enable close short condition", "Close Short")
        self._close_short_operator = self.Param("CloseShortOperator", ">") \
            .SetDisplay("Close Short Operator", "Operator for closing short", "Close Short")
        self._close_short_value = self.Param("CloseShortValue", 0.0) \
            .SetDisplay("Close Short Value", "Value for closing short", "Close Short")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(indicator_test_with_conditions_table_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def _check_condition(self, left, op, right):
        if op == ">":
            return left > right
        elif op == "<":
            return left < right
        elif op == ">=":
            return left >= right
        elif op == "<=":
            return left <= right
        elif op == "=":
            return left == right
        elif op == "!=":
            return left != right
        return False

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        long_entry = self._enable_long_cond.Value and self._check_condition(close, str(self._long_operator.Value), float(self._long_value.Value))
        close_long = self._enable_close_long_cond.Value and self._check_condition(close, str(self._close_long_operator.Value), float(self._close_long_value.Value))
        short_entry = self._enable_short_cond.Value and self._check_condition(close, str(self._short_operator.Value), float(self._short_value.Value))
        close_short = self._enable_close_short_cond.Value and self._check_condition(close, str(self._close_short_operator.Value), float(self._close_short_value.Value))
        if close_long and self.Position > 0:
            self.SellMarket()
        elif close_short and self.Position < 0:
            self.BuyMarket()
        elif long_entry and self.Position <= 0:
            self.BuyMarket()
        elif short_entry and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return indicator_test_with_conditions_table_strategy()
