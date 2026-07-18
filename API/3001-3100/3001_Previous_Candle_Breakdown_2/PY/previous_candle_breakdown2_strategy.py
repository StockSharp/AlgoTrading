import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class previous_candle_breakdown2_strategy(Strategy):
    def __init__(self):
        super(previous_candle_breakdown2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._prev_high = None
        self._prev_low = None
        self._prev_fast = None
        self._prev_slow = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(previous_candle_breakdown2_strategy, self).OnReseted()
        self._prev_high = None
        self._prev_low = None
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(previous_candle_breakdown2_strategy, self).OnStarted2(time)
        self._prev_high = None
        self._prev_low = None
        self._prev_fast = None
        self._prev_slow = None

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._prev_fast = fv
            self._prev_slow = sv
            return
        if self._prev_high is None or self._prev_low is None or self._prev_fast is None or self._prev_slow is None:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._prev_fast = fv
            self._prev_slow = sv
            return
        close = float(candle.ClosePrice)
        bull_trend = fv > sv
        bear_trend = fv < sv
        if bull_trend and close > self._prev_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif bear_trend and close < self._prev_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return previous_candle_breakdown2_strategy()
