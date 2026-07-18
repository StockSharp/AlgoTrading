import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ChoppinessIndex
from StockSharp.Algo.Strategies import Strategy


class choppiness_index_breakout_strategy(Strategy):

    def __init__(self):
        super(choppiness_index_breakout_strategy, self).__init__()

        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._choppiness_period = self.Param("ChoppinessPeriod", 14) \
            .SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Indicators")
        self._choppiness_threshold = self.Param("ChoppinessThreshold", 99.0) \
            .SetDisplay("Choppiness Threshold", "Threshold below which market is trending", "Entry")
        self._high_choppiness_threshold = self.Param("HighChoppinessThreshold", 99.5) \
            .SetDisplay("High Choppiness", "Threshold above which to exit positions", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_choppiness = 100.0
        self._cooldown = 0

    @property
    def MAPeriod(self):
        return self._ma_period.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._ma_period.Value = value

    @property
    def ChoppinessPeriod(self):
        return self._choppiness_period.Value

    @ChoppinessPeriod.setter
    def ChoppinessPeriod(self, value):
        self._choppiness_period.Value = value

    @property
    def ChoppinessThreshold(self):
        return self._choppiness_threshold.Value

    @ChoppinessThreshold.setter
    def ChoppinessThreshold(self, value):
        self._choppiness_threshold.Value = value

    @property
    def HighChoppinessThreshold(self):
        return self._high_choppiness_threshold.Value

    @HighChoppinessThreshold.setter
    def HighChoppinessThreshold(self, value):
        self._high_choppiness_threshold.Value = value

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

    def OnStarted2(self, time):
        super(choppiness_index_breakout_strategy, self).OnStarted2(time)

        self._prev_choppiness = 100.0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        choppiness_index = ChoppinessIndex()
        choppiness_index.Length = self.ChoppinessPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(ma, choppiness_index, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ma_value, choppiness_value):
        if candle.State != CandleStates.Finished:
            return

        ma_f = float(ma_value)
        chop_f = float(choppiness_value)
        chop_threshold = float(self.ChoppinessThreshold)
        high_chop_threshold = float(self.HighChoppinessThreshold)
        cooldown_bars = int(self.CooldownBars)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_choppiness = chop_f
            return

        is_trending = chop_f < chop_threshold
        is_choppy = chop_f > high_chop_threshold
        close = float(candle.ClosePrice)

        if self.Position == 0 and is_trending:
            if close > ma_f:
                self.BuyMarket()
                self._cooldown = cooldown_bars
            elif close < ma_f:
                self.SellMarket()
                self._cooldown = cooldown_bars
        elif self.Position > 0 and is_choppy:
            self.SellMarket()
            self._cooldown = cooldown_bars
        elif self.Position < 0 and is_choppy:
            self.BuyMarket()
            self._cooldown = cooldown_bars

        self._prev_choppiness = chop_f

    def OnReseted(self):
        super(choppiness_index_breakout_strategy, self).OnReseted()
        self._prev_choppiness = 100.0
        self._cooldown = 0

    def CreateClone(self):
        return choppiness_index_breakout_strategy()
