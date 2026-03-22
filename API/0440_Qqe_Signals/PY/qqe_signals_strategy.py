import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class qqe_signals_strategy(Strategy):
    """QQE Signals Strategy."""

    def __init__(self):
        super(qqe_signals_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Length", "RSI period", "QQE")
        self._upper_threshold = self.Param("UpperThreshold", 60.0) \
            .SetDisplay("Upper Threshold", "Bullish threshold", "QQE")
        self._lower_threshold = self.Param("LowerThreshold", 40.0) \
            .SetDisplay("Lower Threshold", "Bearish threshold", "QQE")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._rsi = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(qqe_signals_strategy, self).OnReseted()
        self._rsi = None
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(qqe_signals_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            self._prev_rsi = float(rsi_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = float(rsi_val)
            return

        rsi = float(rsi_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi
            return

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi
            return

        upper = float(self._upper_threshold.Value)
        lower = float(self._lower_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        cross_up = rsi > upper and self._prev_rsi <= upper
        cross_down = rsi < lower and self._prev_rsi >= lower

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and rsi < 50 and self._prev_rsi >= 50:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rsi > 50 and self._prev_rsi <= 50:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_rsi = rsi

    def CreateClone(self):
        return qqe_signals_strategy()
