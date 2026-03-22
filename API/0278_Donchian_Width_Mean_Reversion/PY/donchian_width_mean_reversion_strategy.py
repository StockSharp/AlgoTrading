import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class donchian_width_mean_reversion_strategy(Strategy):
    """
    Donchian width mean reversion strategy.
    Trades contractions and expansions of Donchian Channel width around its recent average.
    """

    def __init__(self):
        super(donchian_width_mean_reversion_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Donchian Channel period", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Lookback period for width statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._donchian = None
        self._width_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_width_mean_reversion_strategy, self).OnReseted()
        self._donchian = None
        lb = int(self._lookback_period.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_width_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._donchian = DonchianChannels()
        self._donchian.Length = int(self._donchian_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._donchian, self._process_donchian).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_donchian(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._donchian.IsFormed:
            return

        upper_band = donchian_value.UpperBand
        lower_band = donchian_value.LowerBand
        if upper_band is None or lower_band is None:
            return

        upper_val = float(upper_band)
        lower_val = float(lower_band)

        width = upper_val - lower_val

        lb = int(self._lookback_period.Value)
        self._width_history[self._current_index] = width
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        avg_width = 0.0
        for i in range(lb):
            avg_width += self._width_history[i]
        avg_width /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._width_history[i] - avg_width
            sum_sq += diff * diff
        std_width = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        narrow_threshold = avg_width - std_width * dm
        wide_threshold = avg_width + std_width * dm

        if self.Position == 0:
            if width < narrow_threshold:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif width > wide_threshold:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and width >= avg_width:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and width <= avg_width:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return donchian_width_mean_reversion_strategy()
