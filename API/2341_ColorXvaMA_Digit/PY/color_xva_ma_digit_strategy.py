import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_xva_ma_digit_strategy(Strategy):
    def __init__(self):
        super(color_xva_ma_digit_strategy, self).__init__()
        self._slow_length = self.Param("SlowLength", 15) \
            .SetDisplay("Slow Length", "EMA period", "Indicators")
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast Length", "JMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_direction = 0

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_xva_ma_digit_strategy, self).OnReseted()
        self._previous_direction = 0

    def OnStarted(self, time):
        super(color_xva_ma_digit_strategy, self).OnStarted(time)
        self._previous_direction = 0
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = int(self.slow_length)
        fast_ma = JurikMovingAverage()
        fast_ma.Length = int(self.fast_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(slow_ma, fast_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, fast_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, slow_value, fast_value):
        if candle.State != CandleStates.Finished:
            return
        slow_value = float(slow_value)
        fast_value = float(fast_value)
        direction = 1 if fast_value > slow_value else -1
        if direction != self._previous_direction and self._previous_direction != 0:
            if direction > 0 and self.Position <= 0:
                self.BuyMarket()
            elif direction < 0 and self.Position >= 0:
                self.SellMarket()
        self._previous_direction = direction

    def CreateClone(self):
        return color_xva_ma_digit_strategy()
