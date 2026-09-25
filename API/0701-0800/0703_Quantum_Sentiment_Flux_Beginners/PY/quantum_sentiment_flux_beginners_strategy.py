import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class quantum_sentiment_flux_beginners_strategy(Strategy):
    def __init__(self):
        super(quantum_sentiment_flux_beginners_strategy, self).__init__()

        self._fast_period = self.Param("FastEmaPeriod", 120).SetGreaterThanZero()
        self._slow_period = self.Param("SlowEmaPeriod", 450).SetGreaterThanZero()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero()
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0).SetGreaterThanZero()
        self._ma_strength_threshold = self.Param("MaStrengthThreshold", 0.25).SetNotNegative()
        self._cooldown_bars = self.Param("CooldownBars", 5).SetNotNegative()
        self._quantity = self.Param("Quantity", 1.0).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._cooldown_remaining = 0
        self._armed_direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(quantum_sentiment_flux_beginners_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._cooldown_remaining = 0
        self._armed_direction = 0

    def OnStarted2(self, time):
        super(quantum_sentiment_flux_beginners_strategy, self).OnStarted2(time)

        fast = ExponentialMovingAverage()
        fast.Length = int(self._fast_period.Value)
        slow = ExponentialMovingAverage()
        slow.Length = int(self._slow_period.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, atr, self._process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        atr = float(atr_value)

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        atr_multiplier = float(self._atr_multiplier.Value)

        if self.Position > 0 and self._entry_price > 0 and self._entry_atr > 0:
            distance = self._entry_atr * atr_multiplier
            if float(candle.LowPrice) <= self._entry_price - distance or float(candle.HighPrice) >= self._entry_price + distance * 2:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._entry_atr = 0.0
                self._cooldown_remaining = int(self._cooldown_bars.Value)
                self._prev_fast = fast
                self._prev_slow = slow
                return
        elif self.Position < 0 and self._entry_price > 0 and self._entry_atr > 0:
            distance = self._entry_atr * atr_multiplier
            if float(candle.HighPrice) >= self._entry_price + distance or float(candle.LowPrice) <= self._entry_price - distance * 2:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._entry_atr = 0.0
                self._cooldown_remaining = int(self._cooldown_bars.Value)
                self._prev_fast = fast
                self._prev_slow = slow
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        if cross_up:
            self._armed_direction = 1
        elif cross_down:
            self._armed_direction = -1

        if (self._armed_direction > 0 and fast <= slow) or (self._armed_direction < 0 and fast >= slow):
            self._armed_direction = 0

        strong_enough = atr > 0 and abs(fast - slow) >= atr * float(self._ma_strength_threshold.Value)

        if self._cooldown_remaining == 0 and self.Position == 0 and strong_enough and self._armed_direction != 0:
            qty = float(self._quantity.Value)
            if self._armed_direction > 0:
                self.BuyMarket(qty)
            else:
                self.SellMarket(qty)

            self._entry_price = float(candle.ClosePrice)
            self._entry_atr = atr
            self._armed_direction = 0

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return quantum_sentiment_flux_beginners_strategy()
