import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class renko_line_break_vs_rsi_strategy(Strategy):
    """Renko-inspired trend detection with RSI pullbacks using standard candles.
    Uses three-bar high/low breakout structure for entries with SL/TP."""

    def __init__(self):
        super(renko_line_break_vs_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 4).SetGreaterThanZero().SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._rsi_shift = self.Param("RsiShift", 10.0).SetGreaterThanZero().SetDisplay("RSI Shift", "Distance from 50 for pullbacks", "Indicators")
        self._take_profit = self.Param("TakeProfit", 1000.0).SetGreaterThanZero().SetDisplay("Take Profit", "TP distance in price", "Risk")
        self._indent = self.Param("Indent", 50.0).SetGreaterThanZero().SetDisplay("Indent", "Indent for breakout levels", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(renko_line_break_vs_rsi_strategy, self).OnReseted()
        self._prev_high1 = 0
        self._prev_high2 = 0
        self._prev_high3 = 0
        self._prev_low1 = 0
        self._prev_low2 = 0
        self._prev_low3 = 0
        self._history_count = 0
        self._active_stop = None
        self._active_tp = None
        self._trend = 0  # 1=up, -1=down, 0=none
        self._prev_bull = None

    def OnStarted(self, time):
        super(renko_line_break_vs_rsi_strategy, self).OnStarted(time)
        self._prev_high1 = 0
        self._prev_high2 = 0
        self._prev_high3 = 0
        self._prev_low1 = 0
        self._prev_low2 = 0
        self._prev_low3 = 0
        self._history_count = 0
        self._active_stop = None
        self._active_tp = None
        self._trend = 0
        self._prev_bull = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        is_bull = candle.ClosePrice > candle.OpenPrice

        # Update trend based on consecutive candle direction
        if self._prev_bull is not None:
            if is_bull:
                self._trend = 1 if self._prev_bull else 2  # 2 = ToUp
            elif candle.ClosePrice < candle.OpenPrice:
                self._trend = -2 if self._prev_bull else -1  # -2 = ToDown
        self._prev_bull = is_bull

        # Manage existing positions
        if self.Position > 0:
            if self._active_tp is not None and candle.HighPrice >= self._active_tp:
                self.SellMarket()
                self._active_stop = None
                self._active_tp = None
            elif self._active_stop is not None and candle.LowPrice <= self._active_stop:
                self.SellMarket()
                self._active_stop = None
                self._active_tp = None
            elif self._trend == -2:  # ToDown
                self.SellMarket()
                self._active_stop = None
                self._active_tp = None
            elif rsi_val > 50 + self._rsi_shift.Value:
                self.SellMarket()
                self._active_stop = None
                self._active_tp = None
        elif self.Position < 0:
            if self._active_tp is not None and candle.LowPrice <= self._active_tp:
                self.BuyMarket()
                self._active_stop = None
                self._active_tp = None
            elif self._active_stop is not None and candle.HighPrice >= self._active_stop:
                self.BuyMarket()
                self._active_stop = None
                self._active_tp = None
            elif self._trend == 2:  # ToUp
                self.BuyMarket()
                self._active_stop = None
                self._active_tp = None
            elif rsi_val < 50 - self._rsi_shift.Value:
                self.BuyMarket()
                self._active_stop = None
                self._active_tp = None

        # New entries
        if self.Position == 0 and self._history_count >= 3:
            indent = self._indent.Value
            tp_dist = self._take_profit.Value

            eff_trend = self._get_effective_trend()

            if eff_trend == 1 and rsi_val <= 50 - self._rsi_shift.Value:
                entry = self._prev_high3 + indent
                stop = min(self._prev_low1, self._prev_low2, self._prev_low3) - indent
                if entry > 0 and stop > 0 and entry > stop:
                    self.BuyMarket()
                    self._active_stop = stop
                    self._active_tp = entry + tp_dist if tp_dist > 0 else None
            elif eff_trend == -1 and rsi_val >= 50 + self._rsi_shift.Value:
                entry = self._prev_low3 - indent
                stop = max(self._prev_high1, self._prev_high2, self._prev_high3) + indent
                if entry > 0 and stop > 0 and entry < stop:
                    self.SellMarket()
                    self._active_stop = stop
                    self._active_tp = entry - tp_dist if tp_dist > 0 else None

        # Update history
        self._prev_high3 = self._prev_high2
        self._prev_high2 = self._prev_high1
        self._prev_high1 = float(candle.HighPrice)
        self._prev_low3 = self._prev_low2
        self._prev_low2 = self._prev_low1
        self._prev_low1 = float(candle.LowPrice)
        if self._history_count < 3:
            self._history_count += 1

    def _get_effective_trend(self):
        if self._trend == 1 or self._trend == -1:
            return self._trend
        if self._history_count >= 3:
            if self._prev_high1 > self._prev_high2 and self._prev_high2 > self._prev_high3:
                return 1
            if self._prev_low1 < self._prev_low2 and self._prev_low2 < self._prev_low3:
                return -1
        return 0

    def CreateClone(self):
        return renko_line_break_vs_rsi_strategy()
