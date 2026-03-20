import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_levels_strategy(Strategy):
    def __init__(self):
        super(simple_levels_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candle timeframe", "General")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback", "Period for high/low levels", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period for trend filter", "Parameters")
        self._highest_high = 0.0
        self._lowest_low = 0.0
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def lookback_period(self):
        return self._lookback_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(simple_levels_strategy, self).OnReseted()
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._bar_count = 0

    def OnStarted(self, time):
        super(simple_levels_strategy, self).OnStarted(time)
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._bar_count = 0
        highest = Highest()
        highest.Length = self.lookback_period
        lowest = Lowest()
        lowest.Length = self.lookback_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, high_level, low_level, ema_value):
        if candle.State != CandleStates.Finished:
            return
        high_level = float(high_level)
        low_level = float(low_level)
        ema_value = float(ema_value)
        self._bar_count += 1
        if self._bar_count < 2:
            self._highest_high = high_level
            self._lowest_low = low_level
            return
        close = float(candle.ClosePrice)
        prev_high = self._highest_high
        prev_low = self._lowest_low
        # Breakout above previous resistance in uptrend
        if close > prev_high and close > ema_value and self.Position <= 0:
            self.BuyMarket()
        # Breakout below previous support in downtrend
        elif close < prev_low and close < ema_value and self.Position >= 0:
            self.SellMarket()
        self._highest_high = high_level
        self._lowest_low = low_level

    def CreateClone(self):
        return simple_levels_strategy()
