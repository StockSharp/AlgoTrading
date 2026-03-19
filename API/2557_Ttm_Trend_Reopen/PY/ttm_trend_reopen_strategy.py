import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ttm_trend_reopen_strategy(Strategy):
    """TTM Trend approximation: Heikin-Ashi color-based entries with SL/TP protection."""
    def __init__(self):
        super(ttm_trend_reopen_strategy, self).__init__()
        self._comp_bars = self.Param("CompBars", 6).SetGreaterThanZero().SetDisplay("Comp Bars", "HA comparison bars", "Indicator")
        self._sl_points = self.Param("StopLossPoints", 1000.0).SetNotNegative().SetDisplay("Stop Loss", "SL in points", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 2000.0).SetNotNegative().SetDisplay("Take Profit", "TP in points", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ttm_trend_reopen_strategy, self).OnReseted()
        self._prev_ha_open = 0
        self._prev_ha_close = 0
        self._history = []
        self._prev_color = -1

    def OnStarted(self, time):
        super(ttm_trend_reopen_strategy, self).OnStarted(time)
        self._prev_ha_open = 0
        self._prev_ha_close = 0
        self._history = []
        self._prev_color = -1

        ema = ExponentialMovingAverage()
        ema.Length = 5

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        sl = self._sl_points.Value
        tp = self._tp_points.Value
        if sl > 0 or tp > 0:
            self.StartProtection(self.CreateProtection(sl if sl > 0 else 0, tp if tp > 0 else 0))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        ha_close = (o + h + l + c) / 4.0
        if self._prev_ha_open == 0:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0

        # Determine base color
        if ha_close > ha_open:
            color = 1  # bullish
        elif ha_close < ha_open:
            color = 0  # bearish
        else:
            color = 2  # neutral

        # Check against history for inside-bar override
        comp = self._comp_bars.Value
        for entry in self._history:
            e_high = max(entry[0], entry[1])
            e_low = min(entry[0], entry[1])
            if ha_open <= e_high and ha_open >= e_low and ha_close <= e_high and ha_close >= e_low:
                color = entry[2]
                break

        self._history.insert(0, (ha_open, ha_close, color))
        if len(self._history) > max(1, comp):
            self._history.pop()

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

        is_bullish = color == 1
        is_bearish = color == 0

        # Close on color flip
        if is_bearish and self.Position > 0:
            self.SellMarket()
        if is_bullish and self.Position < 0:
            self.BuyMarket()

        # Entry on color flip
        if self._prev_color >= 0:
            was_bullish = self._prev_color == 1
            was_bearish = self._prev_color == 0

            if is_bullish and not was_bullish and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif is_bearish and not was_bearish and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_color = color

    def CreateClone(self):
        return ttm_trend_reopen_strategy()
