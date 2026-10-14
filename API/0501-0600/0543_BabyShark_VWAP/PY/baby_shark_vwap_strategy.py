import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

BAND_DEVIATIONS = 2.0


class baby_shark_vwap_strategy(Strategy):
    """
    BabyShark VWAP strategy.
    A rolling VWAP of the typical price over Length candles carries bands two volume-weighted standard deviations away, and an
    RSI of On-Balance Volume confirms extremes. A close below the lower band with OBV RSI below LowerLevel goes long and a close
    above the upper band with OBV RSI above HigherLevel goes short. A long closes when price returns to the VWAP from below and
    a short when it returns from above; a StopLossPercent stop protects both. After a position is closed no new entry is
    taken for Cooldown candles.
    """

    def __init__(self):
        super(baby_shark_vwap_strategy, self).__init__()
        self._length = self.Param("Length", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Rolling VWAP window", "VWAP")
        self._rsi_length = self.Param("RsiLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period applied to OBV", "RSI")
        self._higher_level = self.Param("HigherLevel", 70.0) \
            .SetDisplay("Higher Level", "OBV RSI level that confirms shorts", "RSI")
        self._lower_level = self.Param("LowerLevel", 30.0) \
            .SetDisplay("Lower Level", "OBV RSI level that confirms longs", "RSI")
        self._cooldown = self.Param("Cooldown", 10) \
            .SetNotNegative() \
            .SetDisplay("Cooldown", "Candles to wait after a position closes", "Trading")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.6) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._obv_rsi = None
        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._window = []
        self._obv = 0.0
        self._prev_close = None
        self._bar_index = 0
        self._last_exit_bar = None
        self._was_in_position = False

    def OnReseted(self):
        super(baby_shark_vwap_strategy, self).OnReseted()
        self._obv_rsi = None
        self._reset_state()

    def OnStarted2(self, time):
        super(baby_shark_vwap_strategy, self).OnStarted2(time)

        self._reset_state()

        self._obv_rsi = RelativeStrengthIndex()
        self._obv_rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(Unit(), Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)

        if self._prev_close is not None:
            if close > self._prev_close:
                self._obv += volume
            elif close < self._prev_close:
                self._obv -= volume
        self._prev_close = close

        obv_rsi_value = process_float(self._obv_rsi, self._obv, candle.ServerTime, True)

        typical = (float(candle.HighPrice) + float(candle.LowPrice) + close) / 3.0
        self._window.append((typical, volume))
        length = self._length.Value
        if len(self._window) > length:
            self._window.pop(0)

        # A position that has disappeared since the last candle was closed, by the stop or by an exit.
        if self._was_in_position and self.Position == 0:
            self._last_exit_bar = self._bar_index
        self._was_in_position = self.Position != 0

        if len(self._window) < length or not self._obv_rsi.IsFormed or obv_rsi_value.IsEmpty:
            return

        obv_rsi = float(to_decimal(obv_rsi_value))

        sum_pv = sum(p * v for p, v in self._window)
        sum_v = sum(v for _, v in self._window)
        if sum_v <= 0:
            return

        vwap = sum_pv / sum_v
        sum_sq = sum(v * (p - vwap) * (p - vwap) for p, v in self._window)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        deviation = math.sqrt(max(0.0, sum_sq / sum_v))
        upper = vwap + BAND_DEVIATIONS * deviation
        lower = vwap - BAND_DEVIATIONS * deviation

        if self.Position > 0:
            if close >= vwap:
                self.SellMarket(self.Position)
                self._last_exit_bar = self._bar_index
            return

        if self.Position < 0:
            if close <= vwap:
                self.BuyMarket(-self.Position)
                self._last_exit_bar = self._bar_index
            return

        if self._last_exit_bar is not None and self._bar_index - self._last_exit_bar < self._cooldown.Value:
            return

        if close < lower and obv_rsi < float(self._lower_level.Value):
            self.BuyMarket(self.Volume)
            self._was_in_position = True
        elif close > upper and obv_rsi > float(self._higher_level.Value):
            self.SellMarket(self.Volume)
            self._was_in_position = True

    def CreateClone(self):
        return baby_shark_vwap_strategy()
