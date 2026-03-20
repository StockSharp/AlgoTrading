import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class multi_regression_strategy(Strategy):
    def __init__(self):
        super(multi_regression_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "SMA and StdDev period", "Regression")
        self._risk_multiplier = self.Param("RiskMultiplier", 2.0) \
            .SetDisplay("Risk Multiplier", "StdDev multiplier for bounds", "Risk")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "Common")
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_regression_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(multi_regression_strategy, self).OnStarted(time)
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._initialized = False
        self._cooldown_remaining = 0
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._length.Value
        self._std = StandardDeviation()
        self._std.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._std, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        sv = float(sma_val)
        sdv = float(std_val)
        price = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        rm = float(self._risk_multiplier.Value)
        if not self._initialized:
            self._prev_close = price
            self._prev_upper = sv + sdv * rm
            self._prev_lower = sv - sdv * rm
            self._initialized = True
            return
        upper_bound = sv + sdv * rm
        lower_bound = sv - sdv * rm
        long_entry = self._prev_close < self._prev_lower and price >= lower_bound
        short_entry = self._prev_close > self._prev_upper and price <= upper_bound
        long_exit = self.Position > 0 and (price >= sv or price >= upper_bound)
        short_exit = self.Position < 0 and (price <= sv or price <= lower_bound)
        cd = self._signal_cooldown_bars.Value
        if long_exit:
            self.SellMarket()
            self._cooldown_remaining = cd
        elif short_exit:
            self.BuyMarket()
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and long_entry and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and short_entry and self.Position >= 0:
            self.SellMarket()
            self._cooldown_remaining = cd
        self._prev_close = price
        self._prev_upper = upper_bound
        self._prev_lower = lower_bound

    def CreateClone(self):
        return multi_regression_strategy()
