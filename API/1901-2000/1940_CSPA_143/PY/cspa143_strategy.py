import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class cspa143_strategy(Strategy):

    def __init__(self):
        super(cspa143_strategy, self).__init__()

        self._strength_period = self.Param("StrengthPeriod", 14) \
            .SetDisplay("Strength Period", "RSI period", "Parameters")
        self._threshold = self.Param("Threshold", 18.0) \
            .SetDisplay("Threshold", "RSI distance from 50", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._previous_rsi = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def StrengthPeriod(self):
        return self._strength_period.Value

    @StrengthPeriod.setter
    def StrengthPeriod(self, value):
        self._strength_period.Value = value

    @property
    def Threshold(self):
        return self._threshold.Value

    @Threshold.setter
    def Threshold(self, value):
        self._threshold.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(cspa143_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.StrengthPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        upper = 50.0 + float(self.Threshold)
        lower = 50.0 - float(self.Threshold)

        if not self._is_initialized:
            self._previous_rsi = rsi
            self._is_initialized = True
            return

        long_entry = self._previous_rsi <= upper and rsi > upper
        short_entry = self._previous_rsi >= lower and rsi < lower
        long_exit = self.Position > 0 and self._previous_rsi >= 55.0 and rsi < 55.0
        short_exit = self.Position < 0 and self._previous_rsi <= 45.0 and rsi > 45.0

        if long_exit:
            self.SellMarket(self.Position)
            self._bars_since_trade = 0
        elif short_exit:
            self.BuyMarket(-self.Position)
            self._bars_since_trade = 0
        elif self._bars_since_trade >= self.CooldownBars:
            pos = self.Position
            if long_entry and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif short_entry and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._previous_rsi = rsi

    def OnReseted(self):
        super(cspa143_strategy, self).OnReseted()
        self._previous_rsi = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return cspa143_strategy()
