import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class linear_correlation_oscillator_strategy(Strategy):
    """
    Linear correlation oscillator: computes Pearson correlation of price
    vs time index. Goes long when crossing above entry level, short below.
    """

    def __init__(self):
        super(linear_correlation_oscillator_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Lookback length", "General")
        self._entry_level = self.Param("EntryLevel", 0.08) \
            .SetDisplay("Entry Level", "Absolute level for entry", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Bars between entry signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._prices = []
        self._prev_correlation = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(linear_correlation_oscillator_strategy, self).OnReseted()
        self._prices = []
        self._prev_correlation = 0.0
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(linear_correlation_oscillator_strategy, self).OnStarted(time)
        self._bars_from_signal = self._cooldown_bars.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        length = self._length.Value
        self._prices.append(float(candle.ClosePrice))
        if len(self._prices) > length:
            self._prices.pop(0)

        if len(self._prices) < length:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        correlation = self._calculate_correlation()
        self._bars_from_signal += 1
        entry = self._entry_level.Value

        if self._bars_from_signal >= self._cooldown_bars.Value:
            if self._prev_correlation <= entry and correlation > entry and self.Position <= 0:
                self.BuyMarket()
                self._bars_from_signal = 0
            elif self._prev_correlation >= -entry and correlation < -entry and self.Position >= 0:
                self.SellMarket()
                self._bars_from_signal = 0

        self._prev_correlation = correlation

    def _calculate_correlation(self):
        n = len(self._prices)
        sum_y = 0.0
        sum_y2 = 0.0
        sum_xy = 0.0

        for i in range(n):
            y = self._prices[i]
            x = i + 1
            sum_y += y
            sum_y2 += y * y
            sum_xy += y * x

        sum_x = n * (n + 1.0) / 2.0
        sum_x2 = n * (n + 1.0) * (2.0 * n + 1.0) / 6.0

        numerator = n * sum_xy - sum_x * sum_y
        denom_sq = (n * sum_x2 - sum_x * sum_x) * (n * sum_y2 - sum_y * sum_y)
        if denom_sq <= 0:
            return 0.0
        denominator = math.sqrt(denom_sq)
        return numerator / denominator if denominator != 0 else 0.0

    def CreateClone(self):
        return linear_correlation_oscillator_strategy()
