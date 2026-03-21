import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class silver_trend_color_jfatl_digit_mmrec_strategy(Strategy):
    def __init__(self):
        super(silver_trend_color_jfatl_digit_mmrec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._channel_length = self.Param("ChannelLength", 21) \
            .SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators")

        self._last_trend = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ChannelLength(self):
        return self._channel_length.Value

    def OnReseted(self):
        super(silver_trend_color_jfatl_digit_mmrec_strategy, self).OnReseted()
        self._last_trend = 0

    def OnStarted(self, time):
        super(silver_trend_color_jfatl_digit_mmrec_strategy, self).OnStarted(time)
        self._last_trend = 0

        highest = Highest()
        highest.Length = self.ChannelLength
        lowest = Lowest()
        lowest.Length = self.ChannelLength

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
        rng = hv - lv
        if rng <= 0:
            return
        close = float(candle.ClosePrice)
        mid = (hv + lv) / 2.0
        if close > hv - rng * 0.1:
            self._last_trend = 1
        elif close < lv + rng * 0.1:
            self._last_trend = -1
        if self._last_trend > 0 and close > mid and self.Position <= 0:
            self.BuyMarket()
        elif self._last_trend < 0 and close < mid and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return silver_trend_color_jfatl_digit_mmrec_strategy()
