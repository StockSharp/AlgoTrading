import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class random_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(random_trailing_stop_strategy, self).__init__()
        self._min_stop_level = self.Param("MinStopLevel", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Stop %", "Minimal stop distance percent", "Trading")
        self._trailing_step = self.Param("TrailingStep", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Step %", "Trailing stop adjustment step percent", "Trading")
        self._sleep_bars = self.Param("SleepBars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Sleep Bars", "Pause before next trade in bars", "General")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Simple moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._bars_since_last_trade = 0
        self._stop_price = None

    @property
    def min_stop_level(self):
        return self._min_stop_level.Value
    @property
    def trailing_step(self):
        return self._trailing_step.Value
    @property
    def sleep_bars(self):
        return self._sleep_bars.Value
    @property
    def sma_period(self):
        return self._sma_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(random_trailing_stop_strategy, self).OnReseted()
        self._bars_since_last_trade = 0
        self._stop_price = None

    def OnStarted(self, time):
        super(random_trailing_stop_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            self.DrawIndicator(area, sma)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_last_trade += 1
        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        if self.Position == 0:
            if self._bars_since_last_trade < self.sleep_bars:
                return
            self._stop_price = None
            side = self._get_random_side(candle, sma_val)
            if side == Sides.Buy:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._bars_since_last_trade = 0
            return
        stop_dist = close * float(self.min_stop_level) / 100.0
        trail_dist = close * float(self.trailing_step) / 100.0
        if self._stop_price is None:
            if self.Position > 0:
                self._stop_price = close - stop_dist
            else:
                self._stop_price = close + stop_dist
            return
        if self.Position > 0:
            new_stop = close - stop_dist
            if new_stop - self._stop_price >= trail_dist:
                self._stop_price = new_stop
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._bars_since_last_trade = 0
        elif self.Position < 0:
            new_stop = close + stop_dist
            if self._stop_price - new_stop >= trail_dist:
                self._stop_price = new_stop
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._bars_since_last_trade = 0

    def _get_random_side(self, candle, sma_value):
        rnd = int(abs(candle.OpenTime.Ticks)) % 5
        if float(candle.ClosePrice) > sma_value:
            return Sides.Sell if rnd == 0 else Sides.Buy
        else:
            return Sides.Buy if rnd == 1 else Sides.Sell

    def CreateClone(self):
        return random_trailing_stop_strategy()
