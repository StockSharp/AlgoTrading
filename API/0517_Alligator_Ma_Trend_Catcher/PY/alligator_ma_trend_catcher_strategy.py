import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class alligator_ma_trend_catcher_strategy(Strategy):
    def __init__(self):
        super(alligator_ma_trend_catcher_strategy, self).__init__()
        self._jaw_length = self.Param("JawLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Jaw Length", "Length of jaw SMA", "Alligator")
        self._teeth_length = self.Param("TeethLength", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Teeth Length", "Length of teeth SMA", "Alligator")
        self._lips_length = self.Param("LipsLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lips Length", "Length of lips SMA", "Alligator")
        self._trendline_length = self.Param("TrendlineLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "Length of the EMA trendline", "Trendline")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(alligator_ma_trend_catcher_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(alligator_ma_trend_catcher_strategy, self).OnStarted(time)
        jaw = SmoothedMovingAverage()
        jaw.Length = self._jaw_length.Value
        teeth = SmoothedMovingAverage()
        teeth.Length = self._teeth_length.Value
        lips = SmoothedMovingAverage()
        lips.Length = self._lips_length.Value
        trend = ExponentialMovingAverage()
        trend.Length = self._trendline_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jaw, teeth, lips, trend, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, trend)
            self.DrawIndicator(area, jaw)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, jaw_val, teeth_val, lips_val, trend_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        jaw_v = float(jaw_val)
        teeth_v = float(teeth_val)
        lips_v = float(lips_val)
        trendline = float(trend_val)
        close = float(candle.ClosePrice)
        alligator_up = lips_v > teeth_v and teeth_v > jaw_v
        alligator_down = lips_v < teeth_v and teeth_v < jaw_v
        if alligator_up and close > trendline and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif alligator_down and close < trendline and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return alligator_ma_trend_catcher_strategy()
