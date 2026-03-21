import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class silver_trend_crazy_chart_strategy(Strategy):
    def __init__(self):
        super(silver_trend_crazy_chart_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._length = self.Param("Length", 14) \
            .SetDisplay("Length", "Channel length", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Length(self):
        return self._length.Value

    def OnReseted(self):
        super(silver_trend_crazy_chart_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(silver_trend_crazy_chart_strategy, self).OnStarted(time)
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

        highest = Highest()
        highest.Length = self.Length
        lowest = Lowest()
        lowest.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return
        hv = float(high_value)
        lv = float(low_value)
        if not self._has_prev:
            self._prev_high = hv
            self._prev_low = lv
            self._has_prev = True
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        mid = (hv + lv) / 2.0
        prev_mid = (self._prev_high + self._prev_low) / 2.0
        if close > mid and open_p <= prev_mid and self.Position <= 0:
            self.BuyMarket()
        elif close < mid and open_p >= prev_mid and self.Position >= 0:
            self.SellMarket()
        self._prev_high = hv
        self._prev_low = lv

    def CreateClone(self):
        return silver_trend_crazy_chart_strategy()
