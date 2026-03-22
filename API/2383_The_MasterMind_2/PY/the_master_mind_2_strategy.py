import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class the_master_mind_2_strategy(Strategy):
    """Stochastic + Williams %R with trailing stop and break-even."""
    def __init__(self):
        super(the_master_mind_2_strategy, self).__init__()
        self._stoch_period = self.Param("StochasticPeriod", 50).SetDisplay("Stochastic Period", "Period for Stochastic", "Indicators")
        self._stoch_d = self.Param("StochasticD", 3).SetDisplay("Stochastic %D", "Smoothing of %D line", "Indicators")
        self._wpr_period = self.Param("WilliamsRPeriod", 50).SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
        self._sl_points = self.Param("StopLossPoints", 500.0).SetDisplay("Stop Loss", "Initial SL in price points", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 500.0).SetDisplay("Take Profit", "Initial TP in price points", "Risk")
        self._trail_points = self.Param("TrailingStopPoints", 50.0).SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trail_step = self.Param("TrailingStepPoints", 100.0).SetDisplay("Trailing Step", "Minimum move to adjust trailing", "Risk")
        self._be_points = self.Param("BreakEvenPoints", 150.0).SetDisplay("Break Even", "Move stop to entry after profit", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(the_master_mind_2_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._tp_price = 0
        self._prev_signal = None
        self._prev_wpr = None

    def OnStarted(self, time):
        super(the_master_mind_2_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = 0
        self._tp_price = 0
        self._prev_signal = None
        self._prev_wpr = None

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self._stoch_period.Value
        self._stoch.D.Length = self._stoch_d.Value
        self._wpr = WilliamsR()
        self._wpr.Length = self._wpr_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._stoch, self._wpr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._stoch)
            self.DrawIndicator(area, self._wpr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, stoch_val, wpr_val):
        if candle.State != CandleStates.Finished:
            return

        # Extract %D from stochastic
        signal = None
        inner = stoch_val.InnerValues
        if inner is not None:
            for iv in inner:
                signal = float(iv.Value)
                break
        if signal is None:
            return

        wpr = float(wpr_val)
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        if self._prev_signal is None:
            self._prev_signal = signal
            self._prev_wpr = wpr
            return

        # Manage position
        if self.Position > 0:
            # Break-even
            be = self._be_points.Value
            if be > 0 and close - self._entry_price >= be and (self._stop_price == 0 or self._stop_price < self._entry_price):
                self._stop_price = self._entry_price
            # Trailing
            trail = self._trail_points.Value
            step = self._trail_step.Value
            if trail > 0:
                target = close - trail
                if self._stop_price == 0 or target - self._stop_price >= step:
                    self._stop_price = target
            # Exit check
            if (self._stop_price > 0 and low <= self._stop_price) or (self._tp_price > 0 and high >= self._tp_price):
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._tp_price = 0

        elif self.Position < 0:
            be = self._be_points.Value
            if be > 0 and self._entry_price - close >= be and (self._stop_price == 0 or self._stop_price > self._entry_price):
                self._stop_price = self._entry_price
            trail = self._trail_points.Value
            step = self._trail_step.Value
            if trail > 0:
                target = close + trail
                if self._stop_price == 0 or self._stop_price - target >= step:
                    self._stop_price = target
            if (self._stop_price > 0 and high >= self._stop_price) or (self._tp_price > 0 and low <= self._tp_price):
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._tp_price = 0

        # Entry signals
        sl = self._sl_points.Value
        tp = self._tp_points.Value
        if self._prev_signal >= 20 and signal < 20 and self._prev_wpr >= -80 and wpr < -80 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - sl
            self._tp_price = close + tp
        elif self._prev_signal <= 80 and signal > 80 and self._prev_wpr <= -20 and wpr > -20 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + sl
            self._tp_price = close - tp

        self._prev_signal = signal
        self._prev_wpr = wpr

    def CreateClone(self):
        return the_master_mind_2_strategy()
