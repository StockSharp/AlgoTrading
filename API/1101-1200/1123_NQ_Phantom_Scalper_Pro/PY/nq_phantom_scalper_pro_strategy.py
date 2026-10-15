import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, AverageTrueRange, StandardDeviation, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class nq_phantom_scalper_pro_strategy(Strategy):
    def __init__(self):
        super(nq_phantom_scalper_pro_strategy, self).__init__()
        self._band1_mult = self.Param("Band1Mult", 1.0).SetGreaterThanZero()
        self._atr_stop_mult = self.Param("AtrStopMult", 1.0).SetGreaterThanZero()
        self._use_trend_filter = self.Param("UseTrendFilter", False)
        self._trend_length = self.Param("TrendLength", 50).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", tf(5))

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(nq_phantom_scalper_pro_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = 0
        self._last_signal = None

    def OnStarted2(self, time):
        super(nq_phantom_scalper_pro_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._stop_price = 0
        self._last_signal = None

        vwap = VolumeWeightedMovingAverage()
        atr = AverageTrueRange()
        atr.Length = 14
        std = StandardDeviation()
        std.Length = 20
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self._trend_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(vwap, atr, std, trend_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, vwap_val, atr_val, std_val, trend_val):
        if candle.State != CandleStates.Finished:
            return
        if atr_val <= 0 or std_val <= 0:
            return

        upper1 = vwap_val + std_val * self._band1_mult.Value
        lower1 = vwap_val - std_val * self._band1_mult.Value

        trend_ok_long = not self._use_trend_filter.Value or candle.ClosePrice > trend_val
        trend_ok_short = not self._use_trend_filter.Value or candle.ClosePrice < trend_val

        cooldown = TimeSpan.FromMinutes(360)
        if self._last_signal is not None and (candle.OpenTime - self._last_signal) < cooldown:
            return

        if self.Position > 0 and self._stop_price > 0 and candle.ClosePrice <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0
            self._last_signal = candle.OpenTime
            return
        if self.Position < 0 and self._stop_price > 0 and candle.ClosePrice >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0
            self._last_signal = candle.OpenTime
            return

        if self.Position <= 0 and candle.ClosePrice > upper1 and trend_ok_long:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = self._entry_price - atr_val * self._atr_stop_mult.Value * 3
            self._last_signal = candle.OpenTime
        elif self.Position >= 0 and candle.ClosePrice < lower1 and trend_ok_short:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = self._entry_price + atr_val * self._atr_stop_mult.Value * 3
            self._last_signal = candle.OpenTime

    def CreateClone(self):
        return nq_phantom_scalper_pro_strategy()
