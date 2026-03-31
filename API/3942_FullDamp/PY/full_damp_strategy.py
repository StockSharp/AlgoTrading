import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex

class full_damp_strategy(Strategy):
    def __init__(self):
        super(full_damp_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(full_damp_strategy, self).OnStarted2(time)
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi)

        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi_val
            return

        if self._prev_rsi <= 30 and rsi_val > 30 and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif self._prev_rsi >= 70 and rsi_val < 70 and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_rsi = rsi_val

    def OnReseted(self):
        super(full_damp_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    def CreateClone(self):
        return full_damp_strategy()
