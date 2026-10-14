import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class three_bar_low_strategy(Strategy):
    """
    Three Bar Low Strategy.
    A long opens when the close falls below the lowest close of the previous LowestLength candles and, with UseEmaFilter,
    the close is above the MaPeriod EMA. The long closes when the close rises above the highest close of the previous
    HighestLength candles.
    """

    def __init__(self):
        super(three_bar_low_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 200).SetGreaterThanZero().SetDisplay("MA Period", "EMA period of the filter", "Indicators")
        self._lowest_length = self.Param("LowestLength", 3).SetGreaterThanZero().SetDisplay("Lowest Length", "Previous candles of the lowest close", "Indicators")
        self._highest_length = self.Param("HighestLength", 7).SetGreaterThanZero().SetDisplay("Highest Length", "Previous candles of the highest close", "Indicators")
        self._use_ema_filter = self.Param("UseEmaFilter", False).SetDisplay("Use EMA Filter", "Require the close above the EMA for entries", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._lowest_close = None
        self._highest_close = None
        self._prev_lowest = None
        self._prev_highest = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_bar_low_strategy, self).OnReseted()
        self._prev_lowest = None
        self._prev_highest = None

    def OnStarted2(self, time):
        super(three_bar_low_strategy, self).OnStarted2(time)

        self._prev_lowest = None
        self._prev_highest = None

        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value
        self._lowest_close = Lowest()
        self._lowest_close.Length = self._lowest_length.Value
        self._highest_close = Highest()
        self._highest_close.Length = self._highest_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Both levels are measured on the candles before this one.
        lowest = self._prev_lowest
        highest = self._prev_highest

        current_lowest = float(process_float(self._lowest_close, candle.ClosePrice, candle.OpenTime, True).GetValue[Decimal](None))
        current_highest = float(process_float(self._highest_close, candle.ClosePrice, candle.OpenTime, True).GetValue[Decimal](None))

        if self._lowest_close.IsFormed:
            self._prev_lowest = current_lowest

        if self._highest_close.IsFormed:
            self._prev_highest = current_highest

        if lowest is None or highest is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if close > highest:
                self.SellMarket(self.Position)
            return

        ema_ok = not self._use_ema_filter.Value or (ema_value.IsFormed and close > float(ema_value.GetValue[Decimal](None)))

        if self.Position == 0 and close < lowest and ema_ok:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return three_bar_low_strategy()
