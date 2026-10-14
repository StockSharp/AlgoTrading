import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange, StandardDeviation, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class percent_x_trend_follower_strategy(Strategy):
    """
    PercentX trend follower.
    The oscillator measures the distance of the close from the middle of a Keltner or Bollinger band (MaLength, width TrendMultiplier)
    in percent of the half band width. Its highest and lowest values over LoopbackPeriod form the extremes, and the dynamic range is
    the lowest of those highs and the highest of those lows over OuterLoopback. Crossing above the upper range goes long, crossing below
    the lower range goes short, reversing an opposite position. With UseInitialStop an ATR stop of ReverseMultiplier * ATR is placed
    from the entry close.
    """

    def __init__(self):
        super(percent_x_trend_follower_strategy, self).__init__()
        self._band_type = self.Param("BandType", "Keltner").SetDisplay("Band Type", "Band used to normalize price", "Indicators")
        self._ma_length = self.Param("MaLength", 40).SetGreaterThanZero().SetDisplay("MA Length", "Period of the band middle line and width", "Indicators")
        self._loopback_period = self.Param("LoopbackPeriod", 80).SetGreaterThanZero().SetDisplay("Loopback Period", "Bars for the oscillator extremes", "Indicators")
        self._outer_loopback = self.Param("OuterLoopback", 80).SetGreaterThanZero().SetDisplay("Outer Loopback", "Bars for the dynamic range", "Indicators")
        self._use_initial_stop = self.Param("UseInitialStop", True).SetDisplay("Use Initial Stop", "Use the initial ATR stop", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for the stop", "Risk")
        self._trend_multiplier = self.Param("TrendMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Trend Multiplier", "Band width multiplier", "Indicators")
        self._reverse_multiplier = self.Param("ReverseMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Reverse Multiplier", "ATR multiplier of the initial stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_osc = None
        self._prev_upper = None
        self._prev_lower = None
        self._stop_price = None

    def OnReseted(self):
        super(percent_x_trend_follower_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(percent_x_trend_follower_strategy, self).OnStarted2(time)

        self._reset_state()

        ma_length = self._ma_length.Value
        if str(self._band_type.Value) == "Keltner":
            middle = ExponentialMovingAverage()
            width = AverageTrueRange()
        else:
            middle = SimpleMovingAverage()
            width = StandardDeviation()
        middle.Length = ma_length
        width.Length = ma_length

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        self._osc_highest = Highest()
        self._osc_highest.Length = self._loopback_period.Value
        self._osc_lowest = Lowest()
        self._osc_lowest.Length = self._loopback_period.Value
        self._upper_range = Lowest()
        self._upper_range.Length = self._outer_loopback.Value
        self._lower_range = Highest()
        self._lower_range.Length = self._outer_loopback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(middle, width, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, middle)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, middle, width, atr):
        if candle.State != CandleStates.Finished:
            return

        half_width = width * Decimal(self._trend_multiplier.Value)
        if half_width <= 0:
            return

        close = candle.ClosePrice
        osc = (close - middle) / half_width * Decimal(100)

        highest_value = process_float(self._osc_highest, osc, candle.OpenTime, True)
        lowest_value = process_float(self._osc_lowest, osc, candle.OpenTime, True)

        if not self._osc_highest.IsFormed or not self._osc_lowest.IsFormed:
            return

        upper_value = process_float(self._upper_range, highest_value.GetValue[Decimal](None), candle.OpenTime, True)
        lower_value = process_float(self._lower_range, lowest_value.GetValue[Decimal](None), candle.OpenTime, True)

        if not self._upper_range.IsFormed or not self._lower_range.IsFormed:
            return

        upper = upper_value.GetValue[Decimal](None)
        lower = lower_value.GetValue[Decimal](None)

        prev_osc = self._prev_osc
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_osc = osc
        self._prev_upper = upper
        self._prev_lower = lower

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._stop_price is not None:
            if self.Position > 0 and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                return
            if self.Position < 0 and candle.HighPrice >= self._stop_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                return

        if prev_osc is None or prev_upper is None or prev_lower is None:
            return

        reverse_mult = Decimal(self._reverse_multiplier.Value)

        if prev_osc <= prev_upper and osc > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * reverse_mult if self._use_initial_stop.Value else None
        elif prev_osc >= prev_lower and osc < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * reverse_mult if self._use_initial_stop.Value else None

    def CreateClone(self):
        return percent_x_trend_follower_strategy()
