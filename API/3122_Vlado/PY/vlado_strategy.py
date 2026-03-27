import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vlado_strategy(Strategy):
    """EMA crossover with manual SL/TP management and cooldown."""
    def __init__(self):
        super(vlado_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 14).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._sl = self.Param("StopLossPoints", 200).SetNotNegative().SetDisplay("Stop Loss", "SL in price steps", "Risk")
        self._tp = self.Param("TakeProfitPoints", 400).SetNotNegative().SetDisplay("Take Profit", "TP in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vlado_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vlado_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0
        self._cooldown = 0

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        close = float(candle.ClosePrice)
        step = 1.0
        sl_pts = self._sl.Value
        tp_pts = self._tp.Value

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fv
            self._prev_slow = sv
            return

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if sl_pts > 0 and close <= self._entry_price - sl_pts * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fv
                self._prev_slow = sv
                return
            if tp_pts > 0 and close >= self._entry_price + tp_pts * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fv
                self._prev_slow = sv
                return
        elif self.Position < 0 and self._entry_price > 0:
            if sl_pts > 0 and close >= self._entry_price + sl_pts * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fv
                self._prev_slow = sv
                return
            if tp_pts > 0 and close <= self._entry_price - tp_pts * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 80
                self._prev_fast = fv
                self._prev_slow = sv
                return

        # EMA crossover
        if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 80
        elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 80

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return vlado_strategy()
