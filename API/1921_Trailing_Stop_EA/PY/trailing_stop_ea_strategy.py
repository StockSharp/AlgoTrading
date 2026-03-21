import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trailing_stop_ea_strategy(Strategy):
    """Fast/slow EMA crossover with StartProtection trailing stop."""
    def __init__(self):
        super(trailing_stop_ea_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._trailing_pct = self.Param("TrailingPct", 2.0).SetDisplay("Trailing %", "Trailing stop percentage", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trailing_stop_ea_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._is_first = True

    def OnStarted(self, time):
        super(trailing_stop_ea_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._is_first = True

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ema, self.OnProcess).Start()

        pct = self._trailing_pct.Value
        self.StartProtection(self.CreateProtection(pct * 2, pct))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val):
        if candle.State != CandleStates.Finished:
            return

        inp = DecimalIndicatorValue(self._slow_ema, candle.ClosePrice)
        inp.IsFinal = True
        slow_result = self._slow_ema.Process(inp)
        if not slow_result.IsFormed:
            return
        slow_val = float(slow_result.ToDecimal())
        fast_val = float(fast_val)

        if self._is_first:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._is_first = False
            return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:


            self.BuyMarket()


        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:


            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return trailing_stop_ea_strategy()
