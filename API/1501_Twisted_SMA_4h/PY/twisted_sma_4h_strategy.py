import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class twisted_sma_4h_strategy(Strategy):
    """RSI momentum crossing 50 with EMA histogram filter and cooldown."""
    def __init__(self):
        super(twisted_sma_4h_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twisted_sma_4h_strategy, self).OnReseted()
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(twisted_sma_4h_strategy, self).OnStarted(time)
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = 8
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = 21

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, ema_fast, ema_slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, ema_fast_val, ema_slow_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        fv = float(ema_fast_val)
        sv = float(ema_slow_val)

        if self._prev_rsi == 0 or self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_rsi = rv
            self._prev_fast = fv
            self._prev_slow = sv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rv
            self._prev_fast = fv
            self._prev_slow = sv
            return

        hist = fv - sv
        hist_up = hist > 0
        hist_down = hist < 0

        rsi_cross_up = self._prev_rsi <= 50 and rv > 50
        rsi_cross_down = self._prev_rsi >= 50 and rv < 50

        # Exit
        if self.Position > 0 and rsi_cross_down:
            self.SellMarket()
            self._cooldown = 30
        elif self.Position < 0 and rsi_cross_up:
            self.BuyMarket()
            self._cooldown = 30

        # Entry
        if self.Position == 0:
            if rsi_cross_up and hist_up:
                self.BuyMarket()
                self._cooldown = 30
            elif rsi_cross_down and hist_down:
                self.SellMarket()
                self._cooldown = 30

        self._prev_rsi = rv
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return twisted_sma_4h_strategy()
