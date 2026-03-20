import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_renko_strategy(Strategy):
    def __init__(self):
        super(adaptive_renko_strategy, self).__init__()
        self._volatility_period = self.Param("VolatilityPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Volatility Period", "ATR calculation period", "Indicator") \
            .SetOptimize(5, 20, 1)
        self._multiplier = self.Param("Multiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "ATR multiplier", "Indicator") \
            .SetOptimize(0.5, 2.0, 0.5)
        self._min_brick = self.Param("MinBrickSize", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Brick", "Minimum brick size", "Indicator") \
            .SetOptimize(1.0, 5.0, 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for ATR calculation", "General")

        self._last_brick_price = 0.0
        self._has_brick = False

    @property
    def volatility_period(self):
        return self._volatility_period.Value
    @property
    def multiplier(self):
        return self._multiplier.Value
    @property
    def min_brick_size(self):
        return self._min_brick.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_renko_strategy, self).OnReseted()
        self._last_brick_price = 0.0
        self._has_brick = False

    def OnStarted(self, time):
        super(adaptive_renko_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self.volatility_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
        self.StartProtection(None, None)

    def process_candle(self, candle, atr):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr)
        brick = max(atr_val * float(self.multiplier), float(self.min_brick_size))

        if not self._has_brick:
            self._last_brick_price = float(candle.ClosePrice)
            self._has_brick = True
            return

        diff = float(candle.ClosePrice) - self._last_brick_price

        if diff >= brick:
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            self._last_brick_price = float(candle.ClosePrice)
        elif diff <= -brick:
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
            self._last_brick_price = float(candle.ClosePrice)

    def CreateClone(self):
        return adaptive_renko_strategy()
