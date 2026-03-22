import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

import sys


class macd_long_strategy(Strategy):
    """MACD Long Strategy. RSI lookback + MACD zero crossover."""

    def __init__(self):
        super(macd_long_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 40) \
            .SetDisplay("RSI Oversold", "Oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 60) \
            .SetDisplay("RSI Overbought", "Overbought level", "RSI")
        self._lookback_bars = self.Param("LookbackBars", 20) \
            .SetDisplay("Lookback Bars", "Bars to look back for RSI conditions", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._macd = None
        self._bars_since_oversold = sys.maxsize
        self._bars_since_overbought = sys.maxsize
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_long_strategy, self).OnReseted()
        self._rsi = None
        self._macd = None
        self._bars_since_oversold = sys.maxsize
        self._bars_since_overbought = sys.maxsize
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(macd_long_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._macd, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._macd.IsFormed:
            self._prev_macd = float(macd_val)
            return

        rsi = float(rsi_val)
        macd = float(macd_val)
        rsi_os = int(self._rsi_oversold.Value)
        rsi_ob = int(self._rsi_overbought.Value)

        # Track RSI oversold/overbought
        if rsi <= rsi_os:
            self._bars_since_oversold = 0
        else:
            self._bars_since_oversold = min(self._bars_since_oversold + 1, sys.maxsize - 1)

        if rsi >= rsi_ob:
            self._bars_since_overbought = 0
        else:
            self._bars_since_overbought = min(self._bars_since_overbought + 1, sys.maxsize - 1)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd = macd
            return

        cooldown = int(self._cooldown_bars.Value)
        lookback = int(self._lookback_bars.Value)

        was_oversold = self._bars_since_oversold <= lookback
        was_overbought = self._bars_since_overbought <= lookback

        macd_cross_up = macd > 0 and self._prev_macd <= 0 and self._prev_macd != 0
        macd_cross_down = macd < 0 and self._prev_macd >= 0 and self._prev_macd != 0

        if was_oversold and macd_cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif was_overbought and macd_cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and macd_cross_down:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and macd_cross_up:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_macd = macd

    def CreateClone(self):
        return macd_long_strategy()
