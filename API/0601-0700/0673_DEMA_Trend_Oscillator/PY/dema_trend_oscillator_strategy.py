import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DoubleExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

ATR_LENGTH = 14


class dema_trend_oscillator_strategy(Strategy):
    """
    DEMA trend oscillator strategy.
    The DEMA is normalized against its BaseLength SMA and standard deviation: the z-score is mapped to 0..100 with a logistic curve.
    The bands are the DEMA plus and minus that standard deviation.
    A long opens when the normalized value is above LongThreshold and the whole candle is above the upper band;
    a short opens when it is below ShortThreshold and the whole candle is below the lower band.
    The stop is the opposite band at entry, the target is RiskReward times that risk,
    and an ATR trailing stop (AtrMultiplier times ATR) follows the price.
    """

    def __init__(self):
        super(dema_trend_oscillator_strategy, self).__init__()
        self._dema_period = self.Param("DemaPeriod", 40).SetGreaterThanZero().SetDisplay("DEMA Period", "DEMA period", "Indicators")
        self._base_length = self.Param("BaseLength", 20).SetGreaterThanZero().SetDisplay("Base Length", "Length of the SMA and standard deviation of the DEMA", "Indicators")
        self._long_threshold = self.Param("LongThreshold", 55.0).SetDisplay("Long Threshold", "Normalized value above which longs are allowed", "Signals")
        self._short_threshold = self.Param("ShortThreshold", 45.0).SetDisplay("Short Threshold", "Normalized value below which shorts are allowed", "Signals")
        self._risk_reward = self.Param("RiskReward", 1.5).SetNotNegative().SetDisplay("Risk Reward", "Take profit as a multiple of the band stop distance", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._base = None
        self._std_dev = None
        self._clear_stops()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _clear_stops(self):
        self._stop_price = None
        self._take_price = None
        self._trail_price = None

    def OnReseted(self):
        super(dema_trend_oscillator_strategy, self).OnReseted()
        self._clear_stops()

    def OnStarted2(self, time):
        super(dema_trend_oscillator_strategy, self).OnStarted2(time)

        self._clear_stops()

        dema = DoubleExponentialMovingAverage()
        dema.Length = self._dema_period.Value
        atr = AverageTrueRange()
        atr.Length = ATR_LENGTH
        self._base = SimpleMovingAverage()
        self._base.Length = self._base_length.Value
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self._base_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, dema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not dema_value.IsFormed:
            return

        dema_dec = dema_value.GetValue[Decimal](None)
        base_value = process_float(self._base, dema_dec, candle.ServerTime, True)
        sd_value = process_float(self._std_dev, dema_dec, candle.ServerTime, True)

        if not base_value.IsFormed or not sd_value.IsFormed or not atr_value.IsFormed:
            return

        dema = float(dema_dec)
        basis = float(base_value.GetValue[Decimal](None))
        sd = float(sd_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))

        if sd <= 0:
            return

        z = (dema - basis) / sd
        normalized = 100.0 / (1.0 + math.exp(-z))
        upper = dema + sd
        lower = dema - sd

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr_mult = float(self._atr_multiplier.Value)
        risk_reward = float(self._risk_reward.Value)

        if self.Position > 0:
            if atr_mult > 0:
                trail = close - atr * atr_mult
                self._trail_price = trail if self._trail_price is None else max(self._trail_price, trail)

            if ((self._stop_price is not None and low <= self._stop_price)
                    or (self._take_price is not None and high >= self._take_price)
                    or (self._trail_price is not None and low <= self._trail_price)):
                self.SellMarket(self.Position)
                self._clear_stops()
            return

        if self.Position < 0:
            if atr_mult > 0:
                trail = close + atr * atr_mult
                self._trail_price = trail if self._trail_price is None else min(self._trail_price, trail)

            if ((self._stop_price is not None and high >= self._stop_price)
                    or (self._take_price is not None and low <= self._take_price)
                    or (self._trail_price is not None and high >= self._trail_price)):
                self.BuyMarket(-self.Position)
                self._clear_stops()
            return

        if normalized > float(self._long_threshold.Value) and low > upper:
            self.BuyMarket()
            self._stop_price = lower
            self._take_price = close + (close - lower) * risk_reward if risk_reward > 0 else None
            self._trail_price = None
        elif normalized < float(self._short_threshold.Value) and high < lower:
            self.SellMarket()
            self._stop_price = upper
            self._take_price = close - (upper - close) * risk_reward if risk_reward > 0 else None
            self._trail_price = None

    def CreateClone(self):
        return dema_trend_oscillator_strategy()
