import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adaptive_rsi_strategy(Strategy):
    """
    Adaptive RSI Strategy - uses RSI to compute adaptive smoothing factor,
    trades on turns (local min/max) of the adaptive RSI line.
    """

    def __init__(self):
        super(adaptive_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")

        self._length = self.Param("Length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._arsi_prev = None
        self._arsi_prev_prev = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_rsi_strategy, self).OnReseted()
        self._arsi_prev = None
        self._arsi_prev_prev = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_rsi_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        alpha = 2.0 * abs(rsi_value / 100.0 - 0.5)
        src = float(candle.ClosePrice)

        prev = self._arsi_prev if self._arsi_prev is not None else src
        arsi = alpha * src + (1 - alpha) * prev

        if self._arsi_prev_prev is not None:
            if self._cooldown_remaining > 0:
                self._cooldown_remaining -= 1
                self._arsi_prev_prev = self._arsi_prev
                self._arsi_prev = arsi
                return

            long_condition = self._arsi_prev <= self._arsi_prev_prev and arsi > self._arsi_prev
            short_condition = self._arsi_prev >= self._arsi_prev_prev and arsi < self._arsi_prev

            if long_condition and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
                self._cooldown_remaining = self.CooldownBars
            elif short_condition and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = self.CooldownBars

        self._arsi_prev_prev = self._arsi_prev
        self._arsi_prev = arsi

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adaptive_rsi_strategy()
