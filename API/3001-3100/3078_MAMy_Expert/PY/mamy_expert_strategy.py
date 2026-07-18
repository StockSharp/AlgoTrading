import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class mamy_expert_strategy(Strategy):
    """
    MAMy Expert: weighted MA on close/open/weighted-price crossover signals.
    """

    def __init__(self):
        super(mamy_expert_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 3).SetDisplay("MA Period", "MA length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_close_ma = None
        self._prev_open_ma = None
        self._prev_weighted_ma = None
        self._prev_open_signal = None
        self._prev_close_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mamy_expert_strategy, self).OnReseted()
        self._prev_close_ma = None
        self._prev_open_ma = None
        self._prev_weighted_ma = None
        self._prev_open_signal = None
        self._prev_close_signal = None
        self._close_ma = None
        self._open_ma = None
        self._weighted_ma = None

    def OnStarted2(self, time):
        super(mamy_expert_strategy, self).OnStarted2(time)
        self._close_ma = WeightedMovingAverage()
        self._close_ma.Length = self._ma_period.Value
        self._open_ma = WeightedMovingAverage()
        self._open_ma.Length = self._ma_period.Value
        self._weighted_ma = WeightedMovingAverage()
        self._weighted_ma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._close_ma is None:
            return
        
        from System import Decimal
        close_result = process_float(self._close_ma, candle.ClosePrice, candle.OpenTime, True)
        open_result = process_float(self._open_ma, candle.OpenPrice, candle.OpenTime, True)
        weighted_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice * Decimal(2)) / Decimal(4)
        weighted_result = process_float(self._weighted_ma, weighted_price, candle.OpenTime, True)
        if close_result.IsEmpty or open_result.IsEmpty or weighted_result.IsEmpty:
            return
        close_ma_val = float(close_result)
        open_ma_val = float(open_result)
        weighted_ma_val = float(weighted_result)
        prev_close_ma = self._prev_close_ma
        prev_open_ma = self._prev_open_ma
        prev_weighted_ma = self._prev_weighted_ma
        prev_open_signal = self._prev_open_signal
        prev_close_signal = self._prev_close_signal
        self._prev_close_ma = close_ma_val
        self._prev_open_ma = open_ma_val
        self._prev_weighted_ma = weighted_ma_val
        if not self._close_ma.IsFormed or not self._open_ma.IsFormed or not self._weighted_ma.IsFormed:
            self._prev_open_signal = None
            self._prev_close_signal = None
            return
        close_signal = close_ma_val - weighted_ma_val
        open_signal = 0.0
        if prev_close_ma is not None and prev_open_ma is not None and prev_weighted_ma is not None and prev_close_signal is not None:
            close_decreasing = (close_ma_val < prev_close_ma and weighted_ma_val < prev_weighted_ma and
                               close_ma_val < weighted_ma_val and weighted_ma_val < open_ma_val and
                               prev_weighted_ma < prev_open_ma and close_signal <= prev_close_signal)
            close_increasing = (close_ma_val > prev_close_ma and weighted_ma_val > prev_weighted_ma and
                               close_ma_val > weighted_ma_val and weighted_ma_val > open_ma_val and
                               prev_weighted_ma > prev_open_ma and close_signal >= prev_close_signal)
            if close_decreasing or close_increasing:
                open_signal = (weighted_ma_val - open_ma_val) + (close_ma_val - weighted_ma_val)
        if (prev_open_signal is not None and prev_close_signal is not None and
            open_signal >= 0.0 and open_signal > prev_open_signal and
            close_signal < 0.0 and prev_close_signal >= 0.0):
            close_signal = 0.0
        has_prev_open = prev_open_signal is not None
        has_prev_close = prev_close_signal is not None
        open_buy = has_prev_open and open_signal > 0.0 and prev_open_signal <= 0.0
        open_sell = has_prev_open and open_signal < 0.0 and prev_open_signal >= 0.0
        close_buy = has_prev_close and close_signal < 0.0 and prev_close_signal >= 0.0
        close_sell = has_prev_close and close_signal > 0.0 and prev_close_signal <= 0.0
        self._prev_open_signal = open_signal
        self._prev_close_signal = close_signal
        if self.Position > 0:
            if close_buy:
                self.SellMarket()
        elif self.Position < 0:
            if close_sell:
                self.BuyMarket()
        else:
            if open_buy:
                self.BuyMarket()
            elif open_sell:
                self.SellMarket()

    def CreateClone(self):
        return mamy_expert_strategy()
