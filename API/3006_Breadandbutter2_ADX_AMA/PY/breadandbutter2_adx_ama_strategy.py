import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class breadandbutter2_adx_ama_strategy(Strategy):
    def __init__(self):
        super(breadandbutter2_adx_ama_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._kama_period = self.Param("KamaPeriod", 10) \
            .SetDisplay("KAMA Period", "KAMA lookback", "Indicators")
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")

        self._prev_kama = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def KamaPeriod(self):
        return self._kama_period.Value
    @property
    def FastPeriod(self):
        return self._fast_period.Value
    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(breadandbutter2_adx_ama_strategy, self).OnReseted()
        self._prev_kama = None

    def OnStarted(self, time):
        super(breadandbutter2_adx_ama_strategy, self).OnStarted(time)
        self._prev_kama = None

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
            self._prev_kama = fv
            return
        if self._prev_kama is None:
            self._prev_kama = fv
            return
        prev_above = self._prev_kama > sv
        curr_above = fv > sv
        self._prev_kama = fv
        if not prev_above and curr_above and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif prev_above and not curr_above and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return breadandbutter2_adx_ama_strategy()
