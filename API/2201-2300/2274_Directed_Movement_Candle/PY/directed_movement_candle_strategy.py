import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class directed_movement_candle_strategy(Strategy):
    def __init__(self):
        super(directed_movement_candle_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicator")
        self._high_level = self.Param("HighLevel", 70.0) \
            .SetDisplay("High Level", "Upper threshold", "Indicator")
        self._low_level = self.Param("LowLevel", 30.0) \
            .SetDisplay("Low Level", "Lower threshold", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "Data")
        self._prev_color = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(directed_movement_candle_strategy, self).OnReseted()
        self._prev_color = None

    def OnStarted2(self, time):
        super(directed_movement_candle_strategy, self).OnStarted2(time)
        self._prev_color = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        high_lvl = float(self.high_level)
        low_lvl = float(self.low_level)
        color = 1.0
        if rsi_value >= high_lvl:
            color = 2.0
        elif rsi_value <= low_lvl:
            color = 0.0
        if self._prev_color is None:
            self._prev_color = color
            return
        if color == 2.0 and self._prev_color < 2.0 and self.Position <= 0:
            self.BuyMarket()
        elif color == 0.0 and self._prev_color > 0.0 and self.Position >= 0:
            self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return directed_movement_candle_strategy()
