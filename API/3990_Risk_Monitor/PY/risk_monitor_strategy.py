import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class risk_monitor_strategy(Strategy):
    def __init__(self):
        super(risk_monitor_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._tp_points = self.Param("TakeProfitPoints", 500.0).SetNotNegative().SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl_points = self.Param("StopLossPoints", 500.0).SetNotNegative().SetDisplay("Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(risk_monitor_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = 0

    def OnStarted(self, time):
        super(risk_monitor_strategy, self).OnStarted(time)
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = candle.ClosePrice

        # Check SL/TP for existing positions
        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close <= self._entry_price - self._sl_points.Value:
                self.SellMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close >= self._entry_price + self._tp_points.Value:
                self.SellMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close >= self._entry_price + self._sl_points.Value:
                self.BuyMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close <= self._entry_price - self._tp_points.Value:
                self.BuyMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return

        prev_diff = self._prev_fast - self._prev_slow
        curr_diff = fast_val - slow_val

        if prev_diff <= 0 and curr_diff > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position == 0:
                self.BuyMarket()
                self._entry_price = close
        elif prev_diff >= 0 and curr_diff < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position == 0:
                self.SellMarket()
                self._entry_price = close

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return risk_monitor_strategy()
