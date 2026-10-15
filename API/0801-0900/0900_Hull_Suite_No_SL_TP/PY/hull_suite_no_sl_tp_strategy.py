import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

MODE_HMA = 0
MODE_EHMA = 1
MODE_THMA = 2

class hull_suite_no_sl_tp_strategy(Strategy):
    """
    Hull Suite No SL/TP strategy.
    Computes the Hull-type average selected by Mode on the close: HMA = WMA(2*WMA(n/2) - WMA(n), sqrt(n)), EHMA is the same with
    EMAs, and THMA = WMA(3*WMA(m/3) - WMA(m/2) - WMA(m), m) with m = Length / 2. The strategy goes long when the average is above
    its value two bars ago and short when it is below, reversing an opposite position.
    """

    def __init__(self):
        super(hull_suite_no_sl_tp_strategy, self).__init__()
        self._length = self.Param("Length", 55).SetGreaterThanZero().SetDisplay("Length", "Hull average length", "Indicators")
        self._mode = self.Param("Mode", MODE_HMA).SetDisplay("Mode", "0 Hma, 1 Ehma, 2 Thma", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._first = None
        self._second = None
        self._third = None
        self._smooth = None
        self._prev1 = None
        self._prev2 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_suite_no_sl_tp_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None

    @staticmethod
    def _make(cls, length):
        indicator = cls()
        indicator.Length = max(1, length)
        return indicator

    def OnStarted2(self, time):
        super(hull_suite_no_sl_tp_strategy, self).OnStarted2(time)

        self._prev1 = None
        self._prev2 = None

        length = int(self._length.Value)
        mode = int(self._mode.Value)
        sqrt_length = int(round(math.sqrt(length)))

        if mode == MODE_EHMA:
            self._first = self._make(ExponentialMovingAverage, length // 2)
            self._second = self._make(ExponentialMovingAverage, length)
            self._third = None
            self._smooth = self._make(ExponentialMovingAverage, sqrt_length)
        elif mode == MODE_THMA:
            half = max(1, length // 2)
            self._first = self._make(WeightedMovingAverage, half // 3)
            self._second = self._make(WeightedMovingAverage, half // 2)
            self._third = self._make(WeightedMovingAverage, half)
            self._smooth = self._make(WeightedMovingAverage, half)
        else:
            self._first = self._make(WeightedMovingAverage, length // 2)
            self._second = self._make(WeightedMovingAverage, length)
            self._third = None
            self._smooth = self._make(WeightedMovingAverage, sqrt_length)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._smooth)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        t = candle.OpenTime

        first = float(process_float(self._first, close, t, True))
        second = float(process_float(self._second, close, t, True))
        third = float(process_float(self._third, close, t, True)) if self._third is not None else 0.0

        if not self._first.IsFormed or not self._second.IsFormed or (self._third is not None and not self._third.IsFormed):
            return

        if self._third is not None:
            raw = 3.0 * first - second - third
        else:
            raw = 2.0 * first - second

        ma = float(process_float(self._smooth, Decimal(raw), t, True))
        if not self._smooth.IsFormed:
            return

        ma_two_bars_ago = self._prev2
        self._prev2 = self._prev1
        self._prev1 = ma

        if ma_two_bars_ago is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ma > ma_two_bars_ago and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif ma < ma_two_bars_ago and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return hull_suite_no_sl_tp_strategy()
