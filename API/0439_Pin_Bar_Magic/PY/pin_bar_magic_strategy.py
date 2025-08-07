import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    AverageTrueRange,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class pin_bar_magic_strategy(Strategy):
    """Pin Bar Magic strategy using trend filters and wick analysis.

    Identifies bullish and bearish pin bars within EMA/SMA trends. Orders
    are placed at candle extremes and cancelled after a number of bars if
    not triggered.
    """

    def __init__(self):
        super(pin_bar_magic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(60)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._equity_risk = self.Param("EquityRisk", 3.0).SetDisplay(
            "Equity Risk %", "Equity risk percentage", "Risk Management"
        )
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5).SetDisplay(
            "ATR Multiplier", "Stop loss ATR multiplier", "Risk Management"
        )
        self._slow_sma_len = self.Param("SlowSmaLength", 50).SetDisplay(
            "Slow SMA Period", "Slow SMA period", "Indicators"
        )
        self._med_ema_len = self.Param("MediumEmaLength", 18).SetDisplay(
            "Medium EMA Period", "Medium EMA period", "Indicators"
        )
        self._fast_ema_len = self.Param("FastEmaLength", 6).SetDisplay(
            "Fast EMA Period", "Fast EMA period", "Indicators"
        )
        self._atr_len = self.Param("AtrLength", 14).SetDisplay(
            "ATR Period", "ATR period", "Indicators"
        )
        self._cancel_bars = self.Param("CancelEntryBars", 3).SetDisplay(
            "Cancel Entry Bars", "Cancel entry after X bars", "Strategy"
        )

        self._slow_sma = None
        self._med_ema = None
        self._fast_ema = None
        self._atr = None

        self._bars_since_signal = 0
        self._pending_long = False
        self._pending_short = False
        self._entry_price = 0.0
        self._stop_loss = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pin_bar_magic_strategy, self).OnReseted()
        self._bars_since_signal = 0
        self._pending_long = False
        self._pending_short = False
        self._entry_price = 0.0
        self._stop_loss = 0.0

    def OnStarted(self, time):
        super(pin_bar_magic_strategy, self).OnStarted(time)

        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = self._slow_sma_len.Value
        self._med_ema = ExponentialMovingAverage()
        self._med_ema.Length = self._med_ema_len.Value
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_ema_len.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_len.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._slow_sma, self._med_ema, self._fast_ema, self._atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._slow_sma)
            self.DrawIndicator(area, self._med_ema)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, slow_sma, med_ema, fast_ema, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._slow_sma.IsFormed or not self._med_ema.IsFormed or not self._fast_ema.IsFormed or not self._atr.IsFormed:
            return

        candle_range = candle.HighPrice - candle.LowPrice
        if candle_range == 0:
            return

        bullish_pin = False
        bearish_pin = False
        if candle.ClosePrice > candle.OpenPrice:
            lower_wick = candle.OpenPrice - candle.LowPrice
            bullish_pin = lower_wick > 0.66 * candle_range
            upper_wick = candle.HighPrice - candle.ClosePrice
            bearish_pin = upper_wick > 0.66 * candle_range
        else:
            lower_wick = candle.ClosePrice - candle.LowPrice
            bullish_pin = lower_wick > 0.66 * candle_range
            upper_wick = candle.HighPrice - candle.OpenPrice
            bearish_pin = upper_wick > 0.66 * candle_range

        fan_up = fast_ema > med_ema and med_ema > slow_sma
        fan_dn = fast_ema < med_ema and med_ema < slow_sma

        bull_pierce = (
            (candle.LowPrice < fast_ema and candle.OpenPrice > fast_ema and candle.ClosePrice > fast_ema)
            or (candle.LowPrice < med_ema and candle.OpenPrice > med_ema and candle.ClosePrice > med_ema)
            or (candle.LowPrice < slow_sma and candle.OpenPrice > slow_sma and candle.ClosePrice > slow_sma)
        )
        bear_pierce = (
            (candle.HighPrice > fast_ema and candle.OpenPrice < fast_ema and candle.ClosePrice < fast_ema)
            or (candle.HighPrice > med_ema and candle.OpenPrice < med_ema and candle.ClosePrice < med_ema)
            or (candle.HighPrice > slow_sma and candle.OpenPrice < slow_sma and candle.ClosePrice < slow_sma)
        )

        long_entry = fan_up and bullish_pin and bull_pierce
        short_entry = fan_dn and bearish_pin and bear_pierce

        if self._pending_long:
            self._bars_since_signal += 1
            if self._bars_since_signal > self._cancel_bars.Value:
                self._pending_long = False
                self._bars_since_signal = 0
            elif candle.HighPrice >= self._entry_price and self.Position <= 0:
                risk = self._equity_risk.Value * 0.01 * self.Portfolio.CurrentValue
                units = risk / (self._entry_price - self._stop_loss)
                self.BuyMarket(units)
                self._pending_long = False
                self._bars_since_signal = 0
        if self._pending_short:
            self._bars_since_signal += 1
            if self._bars_since_signal > self._cancel_bars.Value:
                self._pending_short = False
                self._bars_since_signal = 0
            elif candle.LowPrice <= self._entry_price and self.Position >= 0:
                risk = self._equity_risk.Value * 0.01 * self.Portfolio.CurrentValue
                units = risk / (self._stop_loss - self._entry_price)
                self.SellMarket(units)
                self._pending_short = False
                self._bars_since_signal = 0

        if long_entry and not self._pending_long and not self._pending_short and self.Position == 0:
            self._pending_long = True
            self._entry_price = candle.HighPrice
            self._stop_loss = candle.LowPrice - atr_val * self._atr_multiplier.Value
            self._bars_since_signal = 0
        elif short_entry and not self._pending_long and not self._pending_short and self.Position == 0:
            self._pending_short = True
            self._entry_price = candle.LowPrice
            self._stop_loss = candle.HighPrice + atr_val * self._atr_multiplier.Value
            self._bars_since_signal = 0

        prev_fast = self._fast_ema.GetValue(1)
        prev_med = self._med_ema.GetValue(1)
        if self.Position > 0 and fast_ema < med_ema and prev_fast >= prev_med:
            self.ClosePosition()
        elif self.Position < 0 and fast_ema > med_ema and prev_fast <= prev_med:
            self.ClosePosition()

    def CreateClone(self):
        return pin_bar_magic_strategy()
