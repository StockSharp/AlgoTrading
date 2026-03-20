import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class levels_with_trail_strategy(Strategy):
    def __init__(self):
        super(levels_with_trail_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA Period", "Moving average period for level", "Parameters")
        self._trail_pct = self.Param("TrailPct", 1.0) \
            .SetDisplay("Trail %", "Trailing stop percent", "Risk")
        self._entry_price = 0.0
        self._best_price = 0.0
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def trail_pct(self):
        return self._trail_pct.Value

    def OnReseted(self):
        super(levels_with_trail_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._best_price = 0.0
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(levels_with_trail_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._best_price = 0.0
        self._prev_price = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        ma_value = float(ma_value)
        price = float(candle.ClosePrice)
        trail = float(self.trail_pct)
        # Trailing stop management
        if self.Position > 0:
            if price > self._best_price:
                self._best_price = price
            stop_level = self._best_price * (1.0 - trail / 100.0)
            if price <= stop_level:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if price < self._best_price:
                self._best_price = price
            stop_level = self._best_price * (1.0 + trail / 100.0)
            if price >= stop_level:
                self.BuyMarket()
                self._entry_price = 0.0
        if not self._has_prev:
            self._prev_price = price
            self._prev_ma = ma_value
            self._has_prev = True
            return
        # Entry: price crosses above MA
        if self._prev_price < self._prev_ma and price >= ma_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = price
            self._best_price = price
        # Entry: price crosses below MA
        elif self._prev_price > self._prev_ma and price <= ma_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = price
            self._best_price = price
        self._prev_price = price
        self._prev_ma = ma_value

    def CreateClone(self):
        return levels_with_trail_strategy()
