import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class tcp_pivot_reversal_limit_strategy(Strategy):
    def __init__(self):
        super(tcp_pivot_reversal_limit_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(tcp_pivot_reversal_limit_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(tcp_pivot_reversal_limit_strategy, self).OnStarted(time)
        self._has_prev = False
        self._cooldown = 0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()

    def process_candle(self, candle, rsi):
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

    def CreateClone(self):
        return tcp_pivot_reversal_limit_strategy()
