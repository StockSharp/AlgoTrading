import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bias_ratio_strategy(Strategy):
    def __init__(self):
        super(bias_ratio_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_period = self.Param("MaPeriod", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period", "Indicators")
        self._bias_threshold = self.Param("BiasThreshold", 0.015) \
            .SetDisplay("Bias Threshold", "Price deviation ratio from MA", "Trading")
        self._prev_bias_ema = 0.0
        self._prev_bias_sma = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bias_ratio_strategy, self).OnReseted()
        self._prev_bias_ema = 0.0
        self._prev_bias_sma = 0.0

    def OnStarted(self, time):
        super(bias_ratio_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        ema_v = float(ema_val)
        sma_v = float(sma_val)
        if ema_v <= 0 or sma_v <= 0:
            return
        close = float(candle.ClosePrice)
        bias_ema = close / ema_v - 1.0
        bias_sma = close / sma_v - 1.0
        threshold = float(self._bias_threshold.Value)
        long_signal = self._prev_bias_ema <= threshold and bias_ema > threshold
        short_signal = self._prev_bias_sma >= -threshold and bias_sma < -threshold
        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_bias_ema = bias_ema
        self._prev_bias_sma = bias_sma

    def CreateClone(self):
        return bias_ratio_strategy()
