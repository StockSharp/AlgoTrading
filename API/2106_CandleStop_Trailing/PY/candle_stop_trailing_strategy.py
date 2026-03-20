import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class candle_stop_trailing_strategy(Strategy):
    def __init__(self):
        super(candle_stop_trailing_strategy, self).__init__()
        self._trail_period = self.Param("TrailPeriod", 5) \
            .SetDisplay("Trail Period", "Look-back for channel trailing", "Parameters")
        self._fast_ema = self.Param("FastEma", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Parameters")
        self._slow_ema = self.Param("SlowEma", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for analysis", "General")
        self._highest = None
        self._lowest = None
        self._stop_price = 0.0

    @property
    def trail_period(self):
        return self._trail_period.Value
    @property
    def fast_ema(self):
        return self._fast_ema.Value
    @property
    def slow_ema(self):
        return self._slow_ema.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(candle_stop_trailing_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(candle_stop_trailing_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_ema
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_ema
        self._highest = Highest()
        self._highest.Length = self.trail_period
        self._lowest = Lowest()
        self._lowest.Length = self.trail_period
        self.Indicators.Add(self._highest)
        self.Indicators.Add(self._lowest)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast_val)
        slow_val = float(slow_val)
        high_result = self._highest.Process(candle)
        low_result = self._lowest.Process(candle)
        if not high_result.IsFormed or not low_result.IsFormed:
            return
        upper = float(high_result.ToDecimal())
        lower = float(low_result.ToDecimal())

        if self.Position == 0:
            if fast_val > slow_val and float(candle.ClosePrice) > slow_val:
                self.BuyMarket()
                self._stop_price = lower
            elif fast_val < slow_val and float(candle.ClosePrice) < slow_val:
                self.SellMarket()
                self._stop_price = upper
        elif self.Position > 0:
            if lower > self._stop_price:
                self._stop_price = lower
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0.0
        elif self.Position < 0:
            if self._stop_price == 0 or upper < self._stop_price:
                self._stop_price = upper
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0.0

    def CreateClone(self):
        return candle_stop_trailing_strategy()
