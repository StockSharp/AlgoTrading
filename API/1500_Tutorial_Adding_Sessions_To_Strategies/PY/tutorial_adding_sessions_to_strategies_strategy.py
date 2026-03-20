import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class tutorial_adding_sessions_to_strategies_strategy(Strategy):
    """RSI momentum with EMA trend filter: buy on RSI cross above 50 in uptrend."""
    def __init__(self):
        super(tutorial_adding_sessions_to_strategies_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI length", "RSI")
        self._upper = self.Param("Upper", 70.0).SetDisplay("Upper Level", "Overbought", "RSI")
        self._lower = self.Param("Lower", 30.0).SetDisplay("Lower Level", "Oversold", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tutorial_adding_sessions_to_strategies_strategy, self).OnReseted()
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(tutorial_adding_sessions_to_strategies_strategy, self).OnStarted(time)
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
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

    def OnProcess(self, candle, rsi_val, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_val)
        fast = float(fast)
        slow = float(slow)

        if self._prev_rsi == 0 or self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_rsi = rsi_val
            self._prev_fast = fast
            self._prev_slow = slow
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi_val
            self._prev_fast = fast
            self._prev_slow = slow
            return

        hist_up = fast > slow
        hist_down = fast < slow
        rsi_cross_up = self._prev_rsi <= 50 and rsi_val > 50
        rsi_cross_down = self._prev_rsi >= 50 and rsi_val < 50

        if self.Position > 0 and rsi_cross_down:
            self.SellMarket()
            self._cooldown = 30
        elif self.Position < 0 and rsi_cross_up:
            self.BuyMarket()
            self._cooldown = 30

        if self.Position == 0:
            if rsi_cross_up and hist_up:
                self.BuyMarket()
                self._cooldown = 30
            elif rsi_cross_down and hist_down:
                self.SellMarket()
                self._cooldown = 30

        self._prev_rsi = rsi_val
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return tutorial_adding_sessions_to_strategies_strategy()
