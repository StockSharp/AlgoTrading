import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
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

        self._cooldown_remaining = 0

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @property
    def VolPeriod(self):
        return self._vol_period.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(time_series_momentum_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(time_series_momentum_strategy, self).OnStarted(time)

        momentum = RateOfChange()
        momentum.Length = self.MomentumPeriod

        volatility = StandardDeviation()
        volatility.Length = self.VolPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(momentum, volatility, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, momentum_val, vol_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        mv = float(momentum_val)

        if mv > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif mv < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return time_series_momentum_strategy()
