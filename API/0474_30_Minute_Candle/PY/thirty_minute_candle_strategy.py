import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class thirty_minute_candle_strategy(Strategy):
    """Close vs previous close with EMA trend filter and cooldown."""
    def __init__(self):
        super(thirty_minute_candle_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20).SetGreaterThanZero().SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15).SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(thirty_minute_candle_strategy, self).OnReseted()
        self._prev_close = 0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(thirty_minute_candle_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._has_prev = False
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

        close = float(candle.ClosePrice)
        ema_val = float(ema_val)

        if not self._has_prev:
            self._prev_close = close
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            return

        # Buy: close > prev close + above EMA
        if close > self._prev_close and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell: close < prev close + below EMA
        elif close < self._prev_close and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: close drops below EMA
        elif self.Position > 0 and close < ema_val:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: close rises above EMA
        elif self.Position < 0 and close > ema_val:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_close = close

    def CreateClone(self):
        return thirty_minute_candle_strategy()
