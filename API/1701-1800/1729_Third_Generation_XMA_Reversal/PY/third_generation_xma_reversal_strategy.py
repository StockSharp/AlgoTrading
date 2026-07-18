import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class third_generation_xma_reversal_strategy(Strategy):
    def __init__(self):
        super(third_generation_xma_reversal_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetDisplay("MA Length", "Base length for the moving average", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._bar_count = 0

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(third_generation_xma_reversal_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(third_generation_xma_reversal_strategy, self).OnStarted2(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = self.ma_length
        ema2 = ExponentialMovingAverage()
        half = int(self.ma_length / 2)
        ema2.Length = half if half > 0 else 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema1_value, ema2_value):
        if candle.State != CandleStates.Finished:
            return
        # XMA = 2*ema1 - ema2 (third generation concept)
        xma = 2 * ema1_value - ema2_value
        self._bar_count += 1
        if self._bar_count >= 3:
            # Local minimum => buy
            if self._prev1 < self._prev2 and xma > self._prev1 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            # Local maximum => sell
            elif self._prev1 > self._prev2 and xma < self._prev1 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev2 = self._prev1
        self._prev1 = xma

    def CreateClone(self):
        return third_generation_xma_reversal_strategy()
