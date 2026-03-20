import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class qqe_signals_strategy(Strategy):
    """QQE Signals Strategy.
    Uses RSI with threshold crossover for trade signals.
    Buys when RSI crosses above upper threshold.
    Sells when RSI crosses below lower threshold.
    Exits at the 50 midline crossover.
    """

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

        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(qqe_signals_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(qqe_signals_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_val)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_rsi = rsi
            return

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi
            return

        upper = self._upper_threshold.Value
        lower = self._lower_threshold.Value

        # RSI crosses above upper threshold (bullish signal)
        cross_up = rsi > upper and self._prev_rsi <= upper
        # RSI crosses below lower threshold (bearish signal)
        cross_down = rsi < lower and self._prev_rsi >= lower

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: RSI drops below 50
        elif self.Position > 0 and rsi < 50 and self._prev_rsi >= 50:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: RSI rises above 50
        elif self.Position < 0 and rsi > 50 and self._prev_rsi <= 50:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_rsi = rsi

    def CreateClone(self):
        return qqe_signals_strategy()
