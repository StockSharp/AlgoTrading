import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    SimpleMovingAverage,
    ExponentialMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_ema_strategy(Strategy):
    """RSI + EMA strategy using dual moving-average trend filter."""

    def __init__(self):
        super(rsi_ema_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")

        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")

        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")

        self._ma1_type = self.Param("Ma1Type", "EMA") \
            .SetDisplay("MA1 Type", "First moving average type", "Moving Averages")

        self._ma1_length = self.Param("Ma1Length", 150) \
            .SetDisplay("MA1 Length", "First moving average length", "Moving Averages")

        self._ma2_type = self.Param("Ma2Type", "EMA") \
            .SetDisplay("MA2 Type", "Second moving average type", "Moving Averages")

        self._ma2_length = self.Param("Ma2Length", 600) \
            .SetDisplay("MA2 Length", "Second moving average length", "Moving Averages")

        self._rsi = None
        self._ma1 = None
        self._ma2 = None

    # region parameter properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def ma1_type(self):
        return self._ma1_type.Value

    @property
    def ma1_length(self):
        return self._ma1_length.Value

    @property
    def ma2_type(self):
        return self._ma2_type.Value

    @property
    def ma2_length(self):
        return self._ma2_length.Value
    # endregion

    def OnStarted(self, time):
        super(rsi_ema_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        if self.ma1_type.upper() == "SMA":
            self._ma1 = SimpleMovingAverage()
        else:
            self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self.ma1_length

        if self.ma2_type.upper() == "SMA":
            self._ma2 = SimpleMovingAverage()
        else:
            self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self.ma2_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ma1, self._ma2, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, ma1_value, ma2_value):
        if candle.State != CandleStates.Finished:
            return

        if not (self._rsi.IsFormed and self._ma1.IsFormed and self._ma2.IsFormed):
            return

        price = candle.ClosePrice

        if rsi_value < self.rsi_oversold and ma1_value > ma2_value and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if rsi_value > self.rsi_overbought and ma1_value > ma2_value and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

        if self.Position > 0 and rsi_value > self.rsi_overbought:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, abs(self.Position)))

        if self.Position < 0 and rsi_value < self.rsi_oversold:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, abs(self.Position)))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_ema_strategy()
