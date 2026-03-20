import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class triple_ema_crossover_strategy(Strategy):
    """Triple SMA crossover: trade on SMA1/SMA2 cross with cooldown."""
    def __init__(self):
        super(triple_ema_crossover_strategy, self).__init__()
        self._sma1_period = self.Param("Sma1Period", 5).SetDisplay("SMA1 Period", "Short SMA", "Indicators")
        self._sma2_period = self.Param("Sma2Period", 13).SetDisplay("SMA2 Period", "Middle SMA", "Indicators")
        self._sma3_period = self.Param("Sma3Period", 21).SetDisplay("SMA3 Period", "Long SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(triple_ema_crossover_strategy, self).OnReseted()
        self._prev_sma1 = 0
        self._prev_sma2 = 0
        self._prev_sma3 = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(triple_ema_crossover_strategy, self).OnStarted(time)
        self._prev_sma1 = 0
        self._prev_sma2 = 0
        self._prev_sma3 = 0
        self._cooldown = 0

        sma1 = SimpleMovingAverage()
        sma1.Length = self._sma1_period.Value
        sma2 = SimpleMovingAverage()
        sma2.Length = self._sma2_period.Value
        sma3 = SimpleMovingAverage()
        sma3.Length = self._sma3_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma1, sma2, sma3, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma1)
            self.DrawIndicator(area, sma2)
            self.DrawIndicator(area, sma3)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, s1, s2, s3):
        if candle.State != CandleStates.Finished:
            return

        s1 = float(s1)
        s2 = float(s2)
        s3 = float(s3)

        if self._prev_sma1 == 0 or self._prev_sma2 == 0 or self._prev_sma3 == 0:
            self._prev_sma1 = s1
            self._prev_sma2 = s2
            self._prev_sma3 = s3
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_sma1 = s1
            self._prev_sma2 = s2
            self._prev_sma3 = s3
            return

        cross_up = self._prev_sma1 <= self._prev_sma2 and s1 > s2
        cross_down = self._prev_sma1 >= self._prev_sma2 and s1 < s2

        # Exit on opposite cross
        if self.Position > 0 and cross_down:
            self.SellMarket()
            self._cooldown = 20
        elif self.Position < 0 and cross_up:
            self.BuyMarket()
            self._cooldown = 20

        # Entry
        if self.Position == 0:
            if cross_up:
                self.BuyMarket()
                self._cooldown = 20
            elif cross_down:
                self.SellMarket()
                self._cooldown = 20

        self._prev_sma1 = s1
        self._prev_sma2 = s2
        self._prev_sma3 = s3

    def CreateClone(self):
        return triple_ema_crossover_strategy()
