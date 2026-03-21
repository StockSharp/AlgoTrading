import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class adjustable_ma_alternating_extremities_strategy(Strategy):
    def __init__(self):
        super(adjustable_ma_alternating_extremities_strategy, self).__init__()
        self._length = self.Param("Length", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Periods for Bollinger Bands", "General")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Bollinger band width", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._is_upper = None
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
        super(adjustable_ma_alternating_extremities_strategy, self).OnReseted()
        self._is_upper = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adjustable_ma_alternating_extremities_strategy, self).OnStarted(time)
        bands = BollingerBands()
        bands.Length = self._length.Value
        bands.Width = self._multiplier.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bands, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bands)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bb_value
        upper = bb.UpBand
        lower = bb.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if high > upper and self._is_upper is not True:
            self._is_upper = True
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif low < lower and self._is_upper is not False:
            self._is_upper = False
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return adjustable_ma_alternating_extremities_strategy()
