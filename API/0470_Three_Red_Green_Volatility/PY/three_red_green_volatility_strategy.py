import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class three_red_green_volatility_strategy(Strategy):
    """Buy after 3 red candles + ATR > average, sell after 3 green + ATR > average, max hold."""
    def __init__(self):
        super(three_red_green_volatility_strategy, self).__init__()
        self._max_hold = self.Param("MaxTradeDuration", 20).SetGreaterThanZero().SetDisplay("Max Hold Bars", "Maximum bars in position", "Trading")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 12).SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(three_red_green_volatility_strategy, self).OnReseted()
        self._red_count = 0
        self._green_count = 0
        self._bars_since_entry = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_red_green_volatility_strategy, self).OnStarted(time)
        self._red_count = 0
        self._green_count = 0
        self._bars_since_entry = 0
        self._cooldown_remaining = 0

        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value
        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = 30

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_val)
        if atr_val <= 0:
            return

        # Update ATR average manually
        inp = DecimalIndicatorValue(self._atr_avg, atr_val)
        inp.IsFinal = True
        avg_result = self._atr_avg.Process(inp)
        atr_avg_val = float(avg_result.ToDecimal()) if self._atr_avg.IsFormed else atr_val

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        is_red = close < open_p
        is_green = close > open_p

        self._red_count = self._red_count + 1 if is_red else 0
        self._green_count = self._green_count + 1 if is_green else 0

        if self.Position != 0:
            self._bars_since_entry += 1

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        high_vol = atr_val > atr_avg_val * 0.8

        # Buy after 3 red candles + volatility
        if self._red_count >= 3 and high_vol and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bars_since_entry = 0
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell after 3 green candles + volatility
        elif self._green_count >= 3 and high_vol and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bars_since_entry = 0
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: 3 green or max hold
        elif self.Position > 0 and (self._green_count >= 3 or self._bars_since_entry >= self._max_hold.Value):
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: 3 red or max hold
        elif self.Position < 0 and (self._red_count >= 3 or self._bars_since_entry >= self._max_hold.Value):
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return three_red_green_volatility_strategy()
