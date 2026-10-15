import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_delta_strategy(Strategy):
    def __init__(self):
        super(ma_delta_strategy, self).__init__()
        self._delta = self.Param("Delta", 195) \
            .SetDisplay("Delta (pips)", "Hi-Lo threshold in pips", "General")
        self._multiplier = self.Param("Multiplier", 392) \
            .SetDisplay("Multiplier", "Amplifier for MA difference", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 26) \
            .SetDisplay("Fast MA Period", "Period for fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 51) \
            .SetDisplay("Slow MA Period", "Period for slow moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._hi = 0.0
        self._lo = 0.0
        self._is_init = False
        self._trade = 0
        self._delta_step = 0.0
        self._multiplier_factor = 0.0

    @property
    def delta(self):
        return self._delta.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_delta_strategy, self).OnReseted()
        self._hi = 0.0
        self._lo = 0.0
        self._is_init = False
        self._trade = 0
        self._delta_step = 0.0
        self._multiplier_factor = 0.0

    def OnStarted2(self, time):
        super(ma_delta_strategy, self).OnStarted2(time)
        self._hi = 0.0
        self._lo = 0.0
        self._is_init = False
        self._trade = 0
        self._delta_step = float(self.delta) * 0.00001
        self._multiplier_factor = float(self.multiplier) * 0.1
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_ma_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_ma_value, slow_ma_value):
        if candle.State != CandleStates.Finished:
            return
        fast_ma_value = float(fast_ma_value)
        slow_ma_value = float(slow_ma_value)
        diff = self._multiplier_factor * (fast_ma_value - slow_ma_value)
        px = math.pow(diff, 3)
        if not self._is_init:
            self._hi = 0.0
            self._lo = 0.0
            self._trade = 0
            self._is_init = True
        if px > self._hi:
            self._hi = px
            self._lo = self._hi - self._delta_step
            self._trade = 1
        elif px < self._lo:
            self._lo = px
            self._hi = self._lo + self._delta_step
            self._trade = -1
        if self._trade == 1 and self.Position <= 0:
            self.BuyMarket()
        elif self._trade == -1 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ma_delta_strategy()
