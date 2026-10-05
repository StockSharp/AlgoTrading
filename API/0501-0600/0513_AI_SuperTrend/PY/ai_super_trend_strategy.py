import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, WeightedMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class ai_super_trend_strategy(Strategy):
    """
    AI SuperTrend strategy.
    Goes long when the SuperTrend flips up while the WMA of price is above the WMA of the SuperTrend line, and short on the
    mirrored setup, reversing an opposite position. A position is closed when the SuperTrend turns against it or when price
    hits an ATR trailing stop that follows the close at AtrFactor times ATR.
    """

    def __init__(self):
        super(ai_super_trend_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period of the SuperTrend and the trailing stop", "SuperTrend")
        self._atr_factor = self.Param("AtrFactor", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Factor", "ATR multiplier of the SuperTrend and the trailing stop", "SuperTrend")
        self._price_wma_length = self.Param("PriceWmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Price WMA Length", "Period of the price WMA", "Filter")
        self._super_wma_length = self.Param("SuperWmaLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("SuperTrend WMA Length", "Period of the WMA of the SuperTrend line", "Filter")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long entries", "Trading")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._super_wma = None
        self._prev_is_up_trend = None
        self._trailing_stop = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(ai_super_trend_strategy, self).OnReseted()
        self._super_wma = None
        self._prev_is_up_trend = None
        self._trailing_stop = 0.0

    def OnStarted2(self, time):
        super(ai_super_trend_strategy, self).OnStarted2(time)

        self._prev_is_up_trend = None
        self._trailing_stop = 0.0

        super_trend = SuperTrend()
        super_trend.Length = self._atr_period.Value
        super_trend.Multiplier = self._atr_factor.Value
        price_wma = WeightedMovingAverage()
        price_wma.Length = self._price_wma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._super_wma = WeightedMovingAverage()
        self._super_wma.Length = self._super_wma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(super_trend, price_wma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawIndicator(area, price_wma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, super_trend_value, price_wma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not super_trend_value.IsFormed:
            return

        super_line = float(to_decimal(super_trend_value))
        super_wma = float(to_decimal(process_float(self._super_wma, super_line, candle.ServerTime, True)))

        is_up_trend = bool(super_trend_value.IsUpTrend)
        prev_up = self._prev_is_up_trend
        self._prev_is_up_trend = is_up_trend

        if prev_up is None or not price_wma_value.IsFormed or not atr_value.IsFormed or not self._super_wma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price_wma = float(to_decimal(price_wma_value))
        atr = float(to_decimal(atr_value))
        close = float(candle.ClosePrice)
        distance = float(self._atr_factor.Value) * atr

        flip_up = not prev_up and is_up_trend
        flip_down = prev_up and not is_up_trend

        if self._enable_long.Value and flip_up and price_wma > super_wma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trailing_stop = close - distance
        elif self._enable_short.Value and flip_down and price_wma < super_wma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trailing_stop = close + distance
        elif self.Position > 0:
            if float(candle.LowPrice) <= self._trailing_stop or not is_up_trend:
                self.SellMarket(self.Position)
            else:
                self._trailing_stop = max(self._trailing_stop, close - distance)
        elif self.Position < 0:
            if float(candle.HighPrice) >= self._trailing_stop or is_up_trend:
                self.BuyMarket(-self.Position)
            else:
                self._trailing_stop = min(self._trailing_stop, close + distance)

    def CreateClone(self):
        return ai_super_trend_strategy()
