import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class adaptive_rsi_strategy(Strategy):
    """Adaptive RSI Strategy."""

    def __init__(self):
        super(adaptive_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._length = self.Param("Length", 14) \
            .SetDisplay("RSI Length", "RSI period", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._arsi_prev = None
        self._arsi_prev_prev = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_rsi_strategy, self).OnReseted()
        self._arsi_prev = None
        self._arsi_prev_prev = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_rsi_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_v = float(rsi_value)
        alpha = 2.0 * abs(rsi_v / 100.0 - 0.5)
        src = float(candle.ClosePrice)

        prev = self._arsi_prev if self._arsi_prev is not None else src
        arsi = alpha * src + (1 - alpha) * prev

        cooldown = int(self._cooldown_bars.Value)

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
                self._cooldown_remaining = cooldown
            elif short_condition and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown

        self._arsi_prev_prev = self._arsi_prev
        self._arsi_prev = arsi

    def CreateClone(self):
        return adaptive_rsi_strategy()
