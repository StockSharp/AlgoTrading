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

        self._short_ema = None
        self._long_ema = None
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(synthetic_lending_rates_strategy, self).OnReseted()
        self._short_ema = None
        self._long_ema = None
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(synthetic_lending_rates_strategy, self).OnStarted(time)

        self._short_ema = ExponentialMovingAverage()
        self._short_ema.Length = int(self._short_period.Value)

        self._long_ema = ExponentialMovingAverage()
        self._long_ema.Length = int(self._long_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._short_ema, self._long_ema, self._process_candle) \
            .Start()

    def _process_candle(self, candle, short_val, long_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(short_val)
        lv = float(long_val)

        if not self._short_ema.IsFormed or not self._long_ema.IsFormed:
            self._prev_short = sv
            self._prev_long = lv
            return

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
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)
        elif current_intensity < 0 and prev_intensity >= 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)

        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return synthetic_lending_rates_strategy()
