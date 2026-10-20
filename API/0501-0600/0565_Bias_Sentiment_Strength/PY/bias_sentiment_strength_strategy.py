import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

from System import Array
from StockSharp.Messages import Level1Fields
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex,
    StochasticK, AwesomeOscillator, VolumeWeightedMovingAverage, SimpleMovingAverage, Alligator,
    IIndicator, DecimalIndicatorValue)

class bias_sentiment_strength_strategy(Strategy):
    def __init__(self):
        super(bias_sentiment_strength_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay(
            "MacdFast", "Fast MACD EMA length", "Parameters")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay(
            "MacdSlow", "Slow MACD EMA length", "Parameters")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay(
            "MacdSignal", "MACD signal EMA length", "Parameters")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay(
            "RsiPeriod", "Wilder RSI length", "Parameters")
        self._stoch_k = self.Param("StochK", 21).SetGreaterThanZero().SetDisplay(
            "StochK", "Raw stochastic high-low window", "Parameters")
        self._stoch_smooth = self.Param("StochSmooth", 14).SetGreaterThanZero().SetDisplay(
            "StochSmooth", "Stochastic K smoothing length", "Parameters")
        self._stoch_d = self.Param("StochD", 14).SetGreaterThanZero().SetDisplay(
            "StochD", "Smoothed stochastic D length", "Parameters")
        self._ao_short = self.Param("AoShort", 5).SetGreaterThanZero().SetDisplay(
            "AoShort", "Short median-price SMA length", "Parameters")
        self._ao_long = self.Param("AoLong", 34).SetGreaterThanZero().SetDisplay(
            "AoLong", "Long median-price SMA length", "Parameters")
        self._volume_length = self.Param("VolumeLength", 30).SetGreaterThanZero().SetDisplay(
            "VolumeLength", "Close VWMA and SMA length", "Parameters")
        self._bass_threshold = self.Param("BassThreshold", 0.0).SetRange(0.0, float(Decimal.MaxValue)).SetDisplay(
            "BassThreshold", "Minimum absolute aggregate bias", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetGreaterThanZero().SetDisplay(
            "StopLossPercent", "Native filled-entry percentage stop", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay(
            "CandleType", "Finished signal candle interval", "Parameters")
        self._pending = None
        self._smooth_k = self._smooth_d = None
        self._last_bass = None
        self.OrderRegistering += self._remember_order

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macd_signal.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def StochK(self):
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochSmooth(self):
        return self._stoch_smooth.Value

    @StochSmooth.setter
    def StochSmooth(self, value):
        self._stoch_smooth.Value = value

    @property
    def StochD(self):
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def AoShort(self):
        return self._ao_short.Value

    @AoShort.setter
    def AoShort(self, value):
        self._ao_short.Value = value

    @property
    def AoLong(self):
        return self._ao_long.Value

    @AoLong.setter
    def AoLong(self, value):
        self._ao_long.Value = value

    @property
    def VolumeLength(self):
        return self._volume_length.Value

    @VolumeLength.setter
    def VolumeLength(self, value):
        self._volume_length.Value = value

    @property
    def BassThreshold(self):
        return self._bass_threshold.Value

    @BassThreshold.setter
    def BassThreshold(self, value):
        self._bass_threshold.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LastBass(self):
        return self._last_bass

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(bias_sentiment_strength_strategy, self).OnReseted()
        self._pending = None
        self._smooth_k = self._smooth_d = None
        self._last_bass = None

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        if self.MacdFast >= self.MacdSlow or self.AoShort >= self.AoLong:
            raise ValueError("Fast MACD and AO lengths must be shorter than their slow lengths.")
        super(bias_sentiment_strength_strategy, self).OnStarted2(time)
        self._smooth_k, self._smooth_d = SimpleMovingAverage(), SimpleMovingAverage()
        self._smooth_k.Length, self._smooth_d.Length = self.StochSmooth, self.StochD
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length, macd.Macd.LongMa.Length, macd.SignalMa.Length = self.MacdFast, self.MacdSlow, self.MacdSignal
        rsi, stochastic, ao = RelativeStrengthIndex(), StochasticK(), AwesomeOscillator()
        rsi.Length, stochastic.Length = self.RsiPeriod, self.StochK
        ao.ShortMa.Length, ao.LongMa.Length = self.AoShort, self.AoLong
        vwma, sma, gator = VolumeWeightedMovingAverage(), SimpleMovingAverage(), Alligator()
        vwma.Length = sma.Length = self.VolumeLength
        self.StartProtection(Unit(), Unit(float(self.StopLossPercent), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        candles = self.SubscribeCandles(self.CandleType)
        candles.BindEx(Array[IIndicator]([macd, rsi, stochastic, ao, vwma, sma, gator]), self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _process(self, candle, values):
        if candle.State != CandleStates.Finished or not values[2].IsFormed or values[2].IsEmpty:
            return
        raw = DecimalIndicatorValue(self._smooth_k, values[2].GetValue[Decimal](None), candle.OpenTime)
        raw.IsFinal = True
        smooth = self._smooth_k.Process(raw)
        if not self._smooth_k.IsFormed:
            return
        smooth_input = DecimalIndicatorValue(self._smooth_d, smooth.GetValue[Decimal](None), candle.OpenTime)
        smooth_input.IsFinal = True
        average = self._smooth_d.Process(smooth_input)
        if not self._smooth_d.IsFormed or not all(v.IsFormed and not v.IsEmpty for v in values):
            return
        macd, gator = values[0], values[6]
        if macd.Macd is None or macd.Signal is None or gator.Jaw is None or gator.Teeth is None or gator.Lips is None:
            return
        bass = (macd.Macd - macd.Signal) * Decimal(2) + (values[1].GetValue[Decimal](None) - Decimal(50)) / Decimal(5)
        bass += (smooth.GetValue[Decimal](None) - average.GetValue[Decimal](None)) * Decimal(0.15)
        bass += values[3].GetValue[Decimal](None) * Decimal(0.6)
        bass += (gator.Lips - gator.Teeth) + (gator.Teeth - gator.Jaw)
        bass += values[4].GetValue[Decimal](None) - values[5].GetValue[Decimal](None)
        self._last_bass = bass
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        threshold = Decimal(float(self.BassThreshold))
        if bass > threshold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bass < -threshold and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return bias_sentiment_strength_strategy()
