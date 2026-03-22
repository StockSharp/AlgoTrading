import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class london_breakout_classic_strategy(Strategy):
    def __init__(self):
        super(london_breakout_classic_strategy, self).__init__()
        self._channel_length = self.Param("ChannelLength", 20) \
            .SetDisplay("Channel Length", "Period for breakout channel", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(london_breakout_classic_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(london_breakout_classic_strategy, self).OnStarted(time)
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._bars_since_signal = 0
        self._highest = Highest()
        self._highest.Length = self._channel_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._channel_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        hv = float(high_val)
        lv = float(low_val)
        if not self._highest.IsFormed or not self._lowest.IsFormed:
            self._prev_high = hv
            self._prev_low = lv
            return
        if self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_high = hv
            self._prev_low = lv
            return
        close = float(candle.ClosePrice)
        if close > self._prev_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        elif close < self._prev_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        self._prev_high = hv
        self._prev_low = lv

    def CreateClone(self):
        return london_breakout_classic_strategy()
