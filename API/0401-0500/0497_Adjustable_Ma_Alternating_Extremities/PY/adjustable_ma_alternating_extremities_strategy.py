import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

    def OnReseted(self):
        super(adjustable_ma_alternating_extremities_strategy, self).OnReseted()
        self._is_upper = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adjustable_ma_alternating_extremities_strategy, self).OnStarted2(time)
        bands = BollingerBands()
        bands.Length = int(self._length.Value)
        bands.Width = self._multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bands, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bands)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bb_value.UpBand is None or bb_value.LowBand is None:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        cooldown = int(self._cooldown_bars.Value)

        if high > upper and self._is_upper is not True:
            self._is_upper = True
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
                self._cooldown_remaining = cooldown
        elif low < lower and self._is_upper is not False:
            self._is_upper = False
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown

    def CreateClone(self):
        return adjustable_ma_alternating_extremities_strategy()
