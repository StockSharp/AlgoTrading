import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import LinearReg
from StockSharp.Algo.Strategies import Strategy


class linear_cross_trading_strategy(Strategy):
    def __init__(self):
        super(linear_cross_trading_strategy, self).__init__()
        self._length = self.Param("Length", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Regression Length", "Number of bars for linear regression", "Indicator")
        self._slope_threshold = self.Param("SlopeThresholdPercent", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Threshold Pct", "Minimum normalized slope for signals", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")
        self._prev_slope = 0.0
        self._prev_slope_set = False
        self._bars_from_signal = 999999

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(linear_cross_trading_strategy, self).OnReseted()
        self._prev_slope = 0.0
        self._prev_slope_set = False
        self._bars_from_signal = 999999

    def OnStarted2(self, time):
        super(linear_cross_trading_strategy, self).OnStarted2(time)
        lin_reg = LinearReg()
        lin_reg.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lin_reg, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lin_reg)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, lin_reg_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        if close <= 0.0:
            return
        lrv = float(lin_reg_val)
        slope = (close - lrv) / close * 100.0
        if not self._prev_slope_set:
            self._prev_slope = slope
            self._prev_slope_set = True
            return
        self._bars_from_signal += 1
        th = float(self._slope_threshold.Value)
        cd = self._cooldown_bars.Value
        if self._bars_from_signal >= cd:
            if self._prev_slope <= th and slope > th and self.Position <= 0:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif self._prev_slope >= -th and slope < -th and self.Position >= 0:
                self.SellMarket()
                self._bars_from_signal = 0
        self._prev_slope = slope

    def CreateClone(self):
        return linear_cross_trading_strategy()
