import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class silver_trend_color_j_fatl_digit_strategy(Strategy):
    def __init__(self):
        super(silver_trend_color_j_fatl_digit_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._channel_length = self.Param("ChannelLength", 21) \
            .SetDisplay("Channel Length", "Highest/Lowest lookback", "Indicators")
        self._risk_level = self.Param("RiskLevel", 3) \
            .SetDisplay("Risk Level", "Channel threshold tightness", "Logic")

        self._last_trend = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ChannelLength(self):
        return self._channel_length.Value

    @property
    def RiskLevel(self):
        return self._risk_level.Value

    def OnReseted(self):
        super(silver_trend_color_j_fatl_digit_strategy, self).OnReseted()
        self._last_trend = 0

    def OnStarted(self, time):
        super(silver_trend_color_j_fatl_digit_strategy, self).OnStarted(time)
        self._last_trend = 0

        highest = Highest()
        highest.Length = self.ChannelLength + 1
        lowest = Lowest()
        lowest.Length = self.ChannelLength + 1

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

        risk_modifier = 33.0 - self.RiskLevel
        if risk_modifier < 0:
            risk_modifier = 0.0
        if risk_modifier > 33:
            risk_modifier = 33.0

        threshold_pct = risk_modifier / 100.0
        lower_threshold = lv + rng * threshold_pct
        upper_threshold = hv - rng * threshold_pct

        close = float(candle.ClosePrice)

        if close < lower_threshold:
            self._last_trend = -1
        elif close > upper_threshold:
            self._last_trend = 1

        midpoint = (hv + lv) / 2.0

        if self._last_trend > 0 and close > midpoint and self.Position <= 0:
            self.BuyMarket()
        elif self._last_trend < 0 and close < midpoint and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return silver_trend_color_j_fatl_digit_strategy()
