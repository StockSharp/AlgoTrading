import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class time_series_momentum_strategy(Strategy):
    """Time series momentum strategy with volatility scaling."""

    def __init__(self):
        super(time_series_momentum_strategy, self).__init__()

        self._momentum_period = self.Param("MomentumPeriod", 20) \
            .SetDisplay("Momentum Period", "Lookback for momentum calculation", "Parameters")

        self._vol_period = self.Param("VolPeriod", 14) \
            .SetDisplay("Volatility Period", "Period for volatility estimation", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 25) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._momentum = None
        self._volatility = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(time_series_momentum_strategy, self).OnReseted()
        self._momentum = None
        self._volatility = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(time_series_momentum_strategy, self).OnStarted(time)

        self._momentum = RateOfChange()
        self._momentum.Length = int(self._momentum_period.Value)

        self._volatility = StandardDeviation()
        self._volatility.Length = int(self._vol_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._momentum, self._volatility, self._process_candle) \
            .Start()

    def _process_candle(self, candle, momentum_val, vol_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._momentum.IsFormed or not self._volatility.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        mv = float(momentum_val)

        if mv > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)
        elif mv < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return time_series_momentum_strategy()
