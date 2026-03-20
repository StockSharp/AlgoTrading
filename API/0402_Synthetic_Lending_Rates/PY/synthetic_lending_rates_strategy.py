import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class synthetic_lending_rates_strategy(Strategy):
    """Trades based on changes in synthetic lending-rate intensity derived from price momentum."""

    def __init__(self):
        super(synthetic_lending_rates_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 5) \
            .SetDisplay("Short Period", "Short-term momentum period", "Parameters")

        self._long_period = self.Param("LongPeriod", 20) \
            .SetDisplay("Long Period", "Long-term momentum period", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._cooldown_remaining = 0

    @property
    def ShortPeriod(self):
        return self._short_period.Value

    @property
    def LongPeriod(self):
        return self._long_period.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(synthetic_lending_rates_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(synthetic_lending_rates_strategy, self).OnStarted(time)

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self.ShortPeriod

        long_ema = ExponentialMovingAverage()
        long_ema.Length = self.LongPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(short_ema, long_ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, short_val, long_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(short_val)
        lv = float(long_val)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_short = sv
            self._prev_long = lv
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_short = sv
            self._prev_long = lv
            return

        current_intensity = sv - lv
        prev_intensity = self._prev_short - self._prev_long

        if current_intensity > 0 and prev_intensity <= 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif current_intensity < 0 and prev_intensity >= 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return synthetic_lending_rates_strategy()
