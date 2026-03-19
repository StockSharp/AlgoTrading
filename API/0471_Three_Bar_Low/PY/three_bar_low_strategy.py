import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class three_bar_low_strategy(Strategy):
    """Mean reversion: buy below N-bar low, sell above N-bar high, with EMA filter."""
    def __init__(self):
        super(three_bar_low_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50).SetGreaterThanZero().SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._lookback_low = self.Param("LookbackLow", 3).SetGreaterThanZero().SetDisplay("Lookback Low", "Bars for lowest low", "Parameters")
        self._lookback_high = self.Param("LookbackHigh", 7).SetGreaterThanZero().SetDisplay("Lookback High", "Bars for highest high", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(three_bar_low_strategy, self).OnReseted()
        self._lows = []
        self._highs = []
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_bar_low_strategy, self).OnStarted(time)
        self._lows = []
        self._highs = []
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        close = float(candle.ClosePrice)
        lb_low = self._lookback_low.Value
        lb_high = self._lookback_high.Value

        self._lows.append(low)
        self._highs.append(high)
        if len(self._lows) > lb_low + 1:
            self._lows.pop(0)
        if len(self._highs) > lb_high + 1:
            self._highs.pop(0)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if len(self._lows) <= lb_low or len(self._highs) <= lb_high:
            return

        lowest_low = min(self._lows[:-1])
        highest_high = max(self._highs[:-1])

        # Buy: price breaks below previous N-bar low (mean reversion)
        if close < lowest_low and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell: price breaks above previous N-bar high
        elif close > highest_high and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long
        elif self.Position > 0 and close > highest_high:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short
        elif self.Position < 0 and close < lowest_low:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return three_bar_low_strategy()
