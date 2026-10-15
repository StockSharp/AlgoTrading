import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class williams_alligator_atr_strategy(Strategy):
    def __init__(self):
        super(williams_alligator_atr_strategy, self).__init__()
        self._jaw_length = self.Param("JawLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Jaw Length", "Alligator jaw period", "Alligator")
        self._lips_length = self.Param("LipsLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lips Length", "Alligator lips period", "Alligator")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for stop-loss", "ATR")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss", "ATR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_lips_above_jaw = False
        self._prev_lips_below_jaw = False
        self._is_initialized = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_alligator_atr_strategy, self).OnReseted()
        self._prev_lips_above_jaw = False
        self._prev_lips_below_jaw = False
        self._is_initialized = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(williams_alligator_atr_strategy, self).OnStarted2(time)
        jaw = SmoothedMovingAverage()
        jaw.Length = int(self._jaw_length.Value)
        lips = SmoothedMovingAverage()
        lips.Length = int(self._lips_length.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jaw, lips, atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jaw)
            self.DrawIndicator(area, lips)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, jaw_val, lips_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        jaw_v = float(jaw_val)
        lips_v = float(lips_val)
        atr_v = float(atr_val)
        close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_lips_above_jaw = lips_v > jaw_v
            self._prev_lips_below_jaw = lips_v < jaw_v
            self._is_initialized = True
            return

        if not self._is_initialized:
            self._prev_lips_above_jaw = lips_v > jaw_v
            self._prev_lips_below_jaw = lips_v < jaw_v
            self._is_initialized = True
            return

        lips_above_jaw = lips_v > jaw_v
        lips_below_jaw = lips_v < jaw_v
        atr_mult = float(self._atr_multiplier.Value)
        cooldown = int(self._cooldown_bars.Value)

        # Check ATR stop for existing positions
        if self.Position > 0 and self._entry_price > 0 and atr_v > 0:
            if close <= self._entry_price - atr_mult * atr_v:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = cooldown
                self._prev_lips_above_jaw = lips_above_jaw
                self._prev_lips_below_jaw = lips_below_jaw
                return
        elif self.Position < 0 and self._entry_price > 0 and atr_v > 0:
            if close >= self._entry_price + atr_mult * atr_v:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = cooldown
                self._prev_lips_above_jaw = lips_above_jaw
                self._prev_lips_below_jaw = lips_below_jaw
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_lips_above_jaw = lips_above_jaw
            self._prev_lips_below_jaw = lips_below_jaw
            return

        if not self._prev_lips_above_jaw and lips_above_jaw and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        elif not self._prev_lips_below_jaw and lips_below_jaw and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

        self._prev_lips_above_jaw = lips_above_jaw
        self._prev_lips_below_jaw = lips_below_jaw

    def CreateClone(self):
        return williams_alligator_atr_strategy()
