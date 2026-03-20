import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class absolutely_no_lag_lwma_digit_mm_rec_strategy(Strategy):
    def __init__(self):
        super(absolutely_no_lag_lwma_digit_mm_rec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast Length", "Fast WMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 14) \
            .SetDisplay("Slow Length", "Slow WMA period", "Indicators")

        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    def OnReseted(self):
        super(absolutely_no_lag_lwma_digit_mm_rec_strategy, self).OnReseted()
        self._prev_signal = 0

    def OnStarted(self, time):
        super(absolutely_no_lag_lwma_digit_mm_rec_strategy, self).OnStarted(time)
        self._prev_signal = 0

        fast_wma = WeightedMovingAverage()
        fast_wma.Length = self.FastLength
        slow_wma = WeightedMovingAverage()
        slow_wma.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_wma, slow_wma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_wma)
            self.DrawIndicator(area, slow_wma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if fv > sv:
            signal = 1
        elif fv < sv:
            signal = -1
        else:
            signal = self._prev_signal
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
        return absolutely_no_lag_lwma_digit_mm_rec_strategy()
