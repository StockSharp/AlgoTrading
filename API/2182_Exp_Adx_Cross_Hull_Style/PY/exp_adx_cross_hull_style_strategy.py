import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, HullMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_adx_cross_hull_style_strategy(Strategy):
    def __init__(self):
        super(exp_adx_cross_hull_style_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._fast_hull_length = self.Param("FastHullLength", 20) \
            .SetDisplay("Fast Hull Length", "Period of the fast Hull MA", "Indicators")
        self._slow_hull_length = self.Param("SlowHullLength", 50) \
            .SetDisplay("Slow Hull Length", "Period of the slow Hull MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used by the strategy", "General")
        self._prev_plus_di = None
        self._prev_minus_di = None

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def fast_hull_length(self):
        return self._fast_hull_length.Value

    @property
    def slow_hull_length(self):
        return self._slow_hull_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_adx_cross_hull_style_strategy, self).OnReseted()
        self._prev_plus_di = None
        self._prev_minus_di = None

    def OnStarted(self, time):
        super(exp_adx_cross_hull_style_strategy, self).OnStarted(time)

        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        fast_hull = HullMovingAverage()
        fast_hull.Length = self.fast_hull_length
        slow_hull = HullMovingAverage()
        slow_hull.Length = self.slow_hull_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, fast_hull, slow_hull, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_hull)
            self.DrawIndicator(area, slow_hull)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, adx_value, fast_hull_value, slow_hull_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        if plus_di is None or minus_di is None:
            return
        plus_di = float(plus_di)
        minus_di = float(minus_di)

        if not fast_hull_value.IsFormed or not slow_hull_value.IsFormed:
            return

        fast_hull = float(fast_hull_value.GetValue[float]())
        slow_hull = float(slow_hull_value.GetValue[float]())

        if self._prev_plus_di is not None and self._prev_minus_di is not None:
            # Entry: +DI crosses above -DI
            if self._prev_plus_di <= self._prev_minus_di and plus_di > minus_di and self.Position <= 0:
                self.BuyMarket()
            # Entry: -DI crosses above +DI
            elif self._prev_plus_di >= self._prev_minus_di and plus_di < minus_di and self.Position >= 0:
                self.SellMarket()

            # Exit on Hull MA cross
            if self.Position > 0 and fast_hull < slow_hull:
                self.SellMarket()
            elif self.Position < 0 and fast_hull > slow_hull:
                self.BuyMarket()

        self._prev_plus_di = plus_di
        self._prev_minus_di = minus_di

    def CreateClone(self):
        return exp_adx_cross_hull_style_strategy()
