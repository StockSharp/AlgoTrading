import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy

class simplest_de_marker_strategy(Strategy):
    def __init__(self):
        super(simplest_de_marker_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._demarker_period = self.Param("DemarkerPeriod", 14)
        self._oversold = self.Param("Oversold", 0.2)
        self._overbought = self.Param("Overbought", 0.8)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_value = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DemarkerPeriod(self):
        return self._demarker_period.Value

    @DemarkerPeriod.setter
    def DemarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def Oversold(self):
        return self._oversold.Value

    @Oversold.setter
    def Oversold(self, value):
        self._oversold.Value = value

    @property
    def Overbought(self):
        return self._overbought.Value

    @Overbought.setter
    def Overbought(self, value):
        self._overbought.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(simplest_de_marker_strategy, self).OnReseted()
        self._prev_value = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted2(self, time):
        super(simplest_de_marker_strategy, self).OnStarted2(time)
        self._prev_value = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        demarker = DeMarker()
        demarker.Length = self.DemarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(demarker, self._process_candle).Start()

    def _process_candle(self, candle, demarker_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        dm_val = float(demarker_value)

        if self._has_prev:
            if self._prev_value < self.Oversold and dm_val >= self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_value > self.Overbought and dm_val <= self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_value = dm_val
        self._has_prev = True

    def CreateClone(self):
        return simplest_de_marker_strategy()
