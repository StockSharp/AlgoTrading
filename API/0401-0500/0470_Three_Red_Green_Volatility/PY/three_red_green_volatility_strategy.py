import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class three_red_green_volatility_strategy(Strategy):
    """Three Red / Three Green Strategy with volatility filter."""

    def __init__(self):
        super(three_red_green_volatility_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._max_trade_duration = self.Param("MaxTradeDuration", 20) \
            .SetDisplay("Max Hold Bars", "Maximum bars in position", "Trading")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._atr = None
        self._atr_avg = None
        self._red_count = 0
        self._green_count = 0
        self._bars_since_entry = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_red_green_volatility_strategy, self).OnReseted()
        self._atr = None
        self._atr_avg = None
        self._red_count = 0
        self._green_count = 0
        self._bars_since_entry = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_red_green_volatility_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)

        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = 30

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed:
            return

        atr_v = float(atr_val)

        atr_avg_result = process_float(self._atr_avg, atr_val, candle.ServerTime, True)
        atr_avg_val = float(IndicatorHelper.ToDecimal(atr_avg_result)) if self._atr_avg.IsFormed else atr_v

        is_red = candle.ClosePrice < candle.OpenPrice
        is_green = candle.ClosePrice > candle.OpenPrice

        self._red_count = self._red_count + 1 if is_red else 0
        self._green_count = self._green_count + 1 if is_green else 0

        if self.Position != 0:
            self._bars_since_entry += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        max_hold = int(self._max_trade_duration.Value)
        cooldown = int(self._cooldown_bars.Value)
        high_vol = atr_v > atr_avg_val * 0.8

        if self._red_count >= 3 and high_vol and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._bars_since_entry = 0
            self._cooldown_remaining = cooldown
        elif self._green_count >= 3 and high_vol and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._bars_since_entry = 0
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and (self._green_count >= 3 or self._bars_since_entry >= max_hold):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (self._red_count >= 3 or self._bars_since_entry >= max_hold):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return three_red_green_volatility_strategy()
