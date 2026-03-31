import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class triple_sma_spread_strategy(Strategy):
    def __init__(self):
        super(triple_sma_spread_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 9) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._middle_period = self.Param("MiddlePeriod", 14) \
            .SetDisplay("Middle Period", "Middle SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 29) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicators")

        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def MiddlePeriod(self):
        return self._middle_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(triple_sma_spread_strategy, self).OnReseted()
        self._prev_signal = 0

    def OnStarted2(self, time):
        super(triple_sma_spread_strategy, self).OnStarted2(time)
        self._prev_signal = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        fv = float(fast_value)
        sv = float(slow_value)

        signal = 0
        if fv > sv and close > fv:
            signal = 1
        elif fv < sv and close < fv:
            signal = -1

        if signal == self._prev_signal:
            return

        old_signal = self._prev_signal
        self._prev_signal = signal

        if signal == 1 and old_signal <= 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif signal == -1 and old_signal >= 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return triple_sma_spread_strategy()
