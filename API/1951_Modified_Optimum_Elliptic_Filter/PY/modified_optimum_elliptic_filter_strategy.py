import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class modified_optimum_elliptic_filter_strategy(Strategy):

    def __init__(self):
        super(modified_optimum_elliptic_filter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")

        self._prev_filter1 = 0.0
        self._prev_filter2 = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

        self._price0 = 0.0
        self._price1 = 0.0
        self._price2 = 0.0
        self._price3 = 0.0
        self._filter0 = 0.0
        self._filter1 = 0.0
        self._price_count = 0
        self._filter_count = 0
        self._is_formed = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def _compute_filter(self, candle):
        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._price3 = self._price2
        self._price2 = self._price1
        self._price1 = self._price0
        self._price0 = price
        self._price_count = min(self._price_count + 1, 4)

        if self._price_count < 4 or self._filter_count < 2:
            value = price
            self._is_formed = False
        else:
            value = (0.13785 * (2.0 * self._price0 - self._price1)
                     + 0.0007 * (2.0 * self._price1 - self._price2)
                     + 0.13785 * (2.0 * self._price2 - self._price3)
                     + 1.2103 * self._filter0
                     - 0.4867 * self._filter1)
            self._is_formed = True

        self._filter1 = self._filter0
        self._filter0 = value
        self._filter_count = min(self._filter_count + 1, 2)

        return value

    def OnStarted(self, time):
        super(modified_optimum_elliptic_filter_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        filter_value = self._compute_filter(candle)

        if not self._is_formed:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._is_initialized:
            self._prev_filter2 = filter_value
            self._prev_filter1 = filter_value
            self._is_initialized = True
            return

        cross_up = self._prev_filter1 <= self._prev_filter2 and filter_value > self._prev_filter1
        cross_down = self._prev_filter1 >= self._prev_filter2 and filter_value < self._prev_filter1

        if self._bars_since_trade >= self.CooldownBars:
            pos = self.Position
            if cross_up and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif cross_down and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_filter2 = self._prev_filter1
        self._prev_filter1 = filter_value

    def OnReseted(self):
        super(modified_optimum_elliptic_filter_strategy, self).OnReseted()
        self._prev_filter1 = 0.0
        self._prev_filter2 = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars
        self._price0 = 0.0
        self._price1 = 0.0
        self._price2 = 0.0
        self._price3 = 0.0
        self._filter0 = 0.0
        self._filter1 = 0.0
        self._price_count = 0
        self._filter_count = 0
        self._is_formed = False

    def CreateClone(self):
        return modified_optimum_elliptic_filter_strategy()
