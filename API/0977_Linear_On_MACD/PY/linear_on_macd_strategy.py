import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    OnBalanceVolume,
    MovingAverageConvergenceDivergenceSignal,
    LinearRegression,
    ExponentialMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class linear_on_macd_strategy(Strategy):
    def __init__(self):
        super(linear_on_macd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 70) \
            .SetDisplay("Fast Length", "MACD fast period", "General")
        self._slow_length = self.Param("SlowLength", 200) \
            .SetDisplay("Slow Length", "MACD slow period", "General")
        self._signal_length = self.Param("SignalLength", 50) \
            .SetDisplay("Signal Length", "MACD signal period", "General")
        self._lookback = self.Param("Lookback", 140) \
            .SetDisplay("Lookback", "Linear regression lookback", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(linear_on_macd_strategy, self).OnStarted2(time)
        self._obv = OnBalanceVolume()
        self._obv_macd = MovingAverageConvergenceDivergenceSignal()
        self._obv_macd.Macd.ShortMa.Length = self._fast_length.Value
        self._obv_macd.Macd.LongMa.Length = self._slow_length.Value
        self._obv_macd.SignalMa.Length = self._signal_length.Value
        self._price_macd = MovingAverageConvergenceDivergenceSignal()
        self._price_macd.Macd.ShortMa.Length = self._fast_length.Value
        self._price_macd.Macd.LongMa.Length = self._slow_length.Value
        self._price_macd.SignalMa.Length = self._signal_length.Value
        self._price_reg = LinearRegression()
        self._price_reg.Length = self._lookback.Value
        dummy_ema = ExponentialMovingAverage()
        dummy_ema.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._obv, dummy_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, obv_value, dummy_value):
        if candle.State != CandleStates.Finished:
            return
        obv_macd_result = process_float(self._obv_macd, obv_value, candle.ServerTime, True)
        price_macd_result = process_float(self._price_macd, float(candle.ClosePrice), candle.ServerTime, True)
        reg_result = process_float(self._price_reg, float(candle.ClosePrice), candle.ServerTime, True)
        if not self._obv_macd.IsFormed or not self._price_macd.IsFormed or not self._price_reg.IsFormed:
            return
        obv_macd_val = obv_macd_result.Macd
        obv_signal_val = obv_macd_result.Signal
        price_macd_val = price_macd_result.Macd
        price_signal_val = price_macd_result.Signal
        reg_lr = reg_result.LinearReg
        if obv_macd_val is None or obv_signal_val is None:
            return
        if price_macd_val is None or price_signal_val is None:
            return
        if reg_lr is None:
            return
        obv_m = float(obv_macd_val)
        obv_s = float(obv_signal_val)
        price_m = float(price_macd_val)
        price_s = float(price_signal_val)
        predicted = float(reg_lr)
        close = float(candle.ClosePrice)
        long_cond = price_m > price_s and obv_m > obv_s and close > predicted
        short_cond = obv_m < obv_s and price_m < price_s and close < predicted
        if long_cond and self.Position <= 0:
            self.BuyMarket()
        elif short_cond and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return linear_on_macd_strategy()
