import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class trailing_stop_ea_strategy(Strategy):
    """Fast/slow EMA crossover with StartProtection trailing stop."""
    def __init__(self):
        super(trailing_stop_ea_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._trailing_pct = self.Param("TrailingPct", 2.0).SetDisplay("Trailing %", "Trailing stop percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trailing_stop_ea_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted2(self, time):
        super(trailing_stop_ea_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, self._process_candle).Start()

        pct = float(self._trailing_pct.Value)
        self.StartProtection(
            Unit(pct * 2, UnitTypes.Percent),
            Unit(pct, UnitTypes.Percent),
            True
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val):
        if candle.State != CandleStates.Finished:
            return

        inp = DecimalIndicatorValue(self._slow_ema, candle.ClosePrice, candle.OpenTime)
        inp.IsFinal = True
        slow_result = self._slow_ema.Process(inp)
        if not slow_result.IsFormed:
            return
        slow = float(slow_result)
        fast = float(fast_val)

        if self._is_first:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_first = False
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return trailing_stop_ea_strategy()
