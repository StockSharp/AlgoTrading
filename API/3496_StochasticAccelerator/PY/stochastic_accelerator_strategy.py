import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy

class stochastic_accelerator_strategy(Strategy):
    def __init__(self):
        super(stochastic_accelerator_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._period = self.Param("Period", 12)
        self._roc_level = self.Param("RocLevel", 0.2)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_roc = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def RocLevel(self):
        return self._roc_level.Value

    @RocLevel.setter
    def RocLevel(self, value):
        self._roc_level.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(stochastic_accelerator_strategy, self).OnReseted()
        self._prev_roc = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted(self, time):
        super(stochastic_accelerator_strategy, self).OnStarted(time)
        self._prev_roc = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        roc = RateOfChange()
        roc.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(roc, self._process_candle).Start()

    def _process_candle(self, candle, roc_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        roc_val = float(roc_value)

        if self._has_prev:
            if self._prev_roc <= -self.RocLevel and roc_val > -self.RocLevel and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_roc >= self.RocLevel and roc_val < self.RocLevel and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_roc = roc_val
        self._has_prev = True

    def CreateClone(self):
        return stochastic_accelerator_strategy()
