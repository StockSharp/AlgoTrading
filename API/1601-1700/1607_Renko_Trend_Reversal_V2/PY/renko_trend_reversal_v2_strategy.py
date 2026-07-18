import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class renko_trend_reversal_v2_strategy(Strategy):
    def __init__(self):
        super(renko_trend_reversal_v2_strategy, self).__init__()
        self._std_length = self.Param("StdLength", 14) \
            .SetDisplay("StdDev Length", "StdDev period for brick size", "General")
        self._brick_multiplier = self.Param("BrickMultiplier", 0.5) \
            .SetDisplay("Brick Multiplier", "Multiplier for brick size", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._brick_high = 0.0
        self._brick_low = 0.0
        self._is_up_trend = False
        self._has_brick = False

    @property
    def std_length(self):
        return self._std_length.Value

    @property
    def brick_multiplier(self):
        return self._brick_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(renko_trend_reversal_v2_strategy, self).OnReseted()
        self._brick_high = 0.0
        self._brick_low = 0.0
        self._is_up_trend = False
        self._has_brick = False

    def OnStarted2(self, time):
        super(renko_trend_reversal_v2_strategy, self).OnStarted2(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.std_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, std_dev)
            self.DrawOwnTrades(area)

    def on_process(self, candle, std_value):
        if candle.State != CandleStates.Finished:
            return
        if std_value <= 0:
            return
        brick_size = std_value * self.brick_multiplier
        if not self._has_brick:
            self._brick_high = candle.ClosePrice + brick_size
            self._brick_low = candle.ClosePrice - brick_size
            self._is_up_trend = True
            self._has_brick = True
            return
        # Check for trend reversal via brick break
        if candle.ClosePrice >= self._brick_high:
            # Bullish brick formed
            if not self._is_up_trend:
                # Reversal from down to up
                if self.Position <= 0:
                    self.BuyMarket()
            self._is_up_trend = True
            self._brick_high = candle.ClosePrice + brick_size
            self._brick_low = candle.ClosePrice - brick_size
        elif candle.ClosePrice <= self._brick_low:
            # Bearish brick formed
            if self._is_up_trend:
                # Reversal from up to down
                if self.Position >= 0:
                    self.SellMarket()
            self._is_up_trend = False
            self._brick_high = candle.ClosePrice + brick_size
            self._brick_low = candle.ClosePrice - brick_size

    def CreateClone(self):
        return renko_trend_reversal_v2_strategy()
