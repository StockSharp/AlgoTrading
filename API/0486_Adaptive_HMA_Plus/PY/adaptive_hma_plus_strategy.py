import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_hma_plus_strategy(Strategy):
    """Adaptive HMA Plus Strategy."""

    def __init__(self):
        super(adaptive_hma_plus_strategy, self).__init__()

        self._hma_length = self.Param("HmaLength", 20) \
            .SetDisplay("HMA Length", "Hull Moving Average period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prev_hma = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_hma_plus_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_hma_plus_strategy, self).OnStarted(time)

        hma = HullMovingAverage()
        hma.Length = int(self._hma_length.Value)
        atr_short = AverageTrueRange()
        atr_short.Length = 14
        atr_long = AverageTrueRange()
        atr_long.Length = 46

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr_short, atr_long, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, hma_value, atr_short_value, atr_long_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_hma = float(hma_value)
            return

        hma_v = float(hma_value)

        if self._prev_hma == 0:
            self._prev_hma = hma_v
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_hma = hma_v
            return

        slope = hma_v - self._prev_hma
        vol_expanding = float(atr_short_value) > float(atr_long_value)
        cooldown = int(self._cooldown_bars.Value)

        if slope > 0 and vol_expanding and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif slope < 0 and vol_expanding and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and slope <= 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and slope >= 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_hma = hma_v

    def CreateClone(self):
        return adaptive_hma_plus_strategy()
