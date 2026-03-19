import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adaptive_hma_plus_strategy(Strategy):
    """
    Strategy based on Hull Moving Average slope with ATR volatility filter.
    Enters long when HMA slope is positive and volatility is expanding.
    Enters short when HMA slope is negative and volatility is expanding.
    """

    def __init__(self):
        super(adaptive_hma_plus_strategy, self).__init__()

        self._hma_length = self.Param("HmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Length", "Hull Moving Average period", "General")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prev_hma = 0.0
        self._cooldown_remaining = 0

    @property
    def HmaLength(self):
        return self._hma_length.Value

    @HmaLength.setter
    def HmaLength(self, value):
        self._hma_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_hma_plus_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_hma_plus_strategy, self).OnStarted(time)

        hma = HullMovingAverage()
        hma.Length = self.HmaLength
        atr_short = AverageTrueRange()
        atr_short.Length = 14
        atr_long = AverageTrueRange()
        atr_long.Length = 46

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(hma, atr_short, atr_long, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, hma_value, atr_short_value, atr_long_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_hma = hma_value
            return

        if self._prev_hma == 0:
            self._prev_hma = hma_value
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_hma = hma_value
            return

        slope = hma_value - self._prev_hma
        vol_expanding = atr_short_value > atr_long_value

        # Buy: HMA slope positive + volatility expanding
        if slope > 0 and vol_expanding and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars
        # Sell: HMA slope negative + volatility expanding
        elif slope < 0 and vol_expanding and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars
        # Exit long: slope turns negative
        elif self.Position > 0 and slope <= 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = self.CooldownBars
        # Exit short: slope turns positive
        elif self.Position < 0 and slope >= 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = self.CooldownBars

        self._prev_hma = hma_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adaptive_hma_plus_strategy()
