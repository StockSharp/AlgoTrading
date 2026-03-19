import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ten_pips_eurusd_strategy(Strategy):
    """Breakout above prev high / below prev low with ATR-based SL/TP and trailing."""
    def __init__(self):
        super(ten_pips_eurusd_strategy, self).__init__()
        self._sl_mult = self.Param("StopLossMult", 1.0).SetGreaterThanZero().SetDisplay("SL Mult", "Stop loss ATR multiplier", "Risk")
        self._tp_mult = self.Param("TakeProfitMult", 2.0).SetGreaterThanZero().SetDisplay("TP Mult", "Take profit ATR multiplier", "Risk")
        self._trail_mult = self.Param("TrailingMult", 0.8).SetGreaterThanZero().SetDisplay("Trail Mult", "Trailing stop ATR multiplier", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR calculation length", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ten_pips_eurusd_strategy, self).OnReseted()
        self._prev_high = 0
        self._prev_low = 0
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0
        self._has_prev = False

    def OnStarted(self, time):
        super(ten_pips_eurusd_strategy, self).OnStarted(time)
        self._prev_high = 0
        self._prev_low = 0
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0
        self._has_prev = False

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_val)
        if atr_val <= 0:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return

        close = float(candle.ClosePrice)

        # Manage position
        if self.Position > 0:
            trail = close - self._trail_mult.Value * atr_val
            if trail > self._stop_price:
                self._stop_price = trail
            if close <= self._stop_price or (self._take_price > 0 and close >= self._take_price):
                self.SellMarket()
                self._stop_price = 0
                self._take_price = 0
                self._entry_price = 0
        elif self.Position < 0:
            trail = close + self._trail_mult.Value * atr_val
            if self._stop_price == 0 or trail < self._stop_price:
                self._stop_price = trail
            if close >= self._stop_price or (self._take_price > 0 and close <= self._take_price):
                self.BuyMarket()
                self._stop_price = 0
                self._take_price = 0
                self._entry_price = 0

        # Entry on breakout
        if self._has_prev and self.Position == 0:
            if close > self._prev_high + atr_val * 0.5:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - self._sl_mult.Value * atr_val
                self._take_price = close + self._tp_mult.Value * atr_val
            elif close < self._prev_low - atr_val * 0.5:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + self._sl_mult.Value * atr_val
                self._take_price = close - self._tp_mult.Value * atr_val

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_prev = True

    def CreateClone(self):
        return ten_pips_eurusd_strategy()
