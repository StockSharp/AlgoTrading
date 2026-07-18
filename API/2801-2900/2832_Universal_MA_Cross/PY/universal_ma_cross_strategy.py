import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class universal_ma_cross_strategy(Strategy):
    """Universal MA crossover with manual indicator processing, trailing, SL/TP, and time filter."""
    def __init__(self):
        super(universal_ma_cross_strategy, self).__init__()
        self._fast_period = self.Param("FastMaPeriod", 10).SetGreaterThanZero().SetDisplay("Fast MA Period", "Fast MA length", "Indicators")
        self._slow_period = self.Param("SlowMaPeriod", 80).SetGreaterThanZero().SetDisplay("Slow MA Period", "Slow MA length", "Indicators")
        self._sl = self.Param("StopLoss", 0).SetDisplay("Stop Loss", "SL distance in price", "Risk")
        self._tp = self.Param("TakeProfit", 0).SetDisplay("Take Profit", "TP distance in price", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 0).SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 0).SetDisplay("Trailing Step", "Additional move before trailing", "Risk")
        self._min_cross_dist = self.Param("MinCrossDistance", 0).SetDisplay("Min Cross Distance", "Min distance between averages", "Filters")
        self._stop_and_reverse = self.Param("StopAndReverse", True).SetDisplay("Stop And Reverse", "Reverse on opposite signal", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(universal_ma_cross_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._last_trade = 0  # 0=none, 1=long, -1=short

    def OnStarted2(self, time):
        super(universal_ma_cross_strategy, self).OnStarted2(time)
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._last_trade = 0

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self._fast_period.Value
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Manage existing position
        if self.Position != 0:
            self._update_trailing(close)
            if self.Position > 0:
                if self._stop_price is not None and low <= self._stop_price:
                    self.SellMarket()
                    self._reset_protection()
                elif self._take_price is not None and high >= self._take_price:
                    self.SellMarket()
                    self._reset_protection()
            elif self.Position < 0:
                if self._stop_price is not None and high >= self._stop_price:
                    self.BuyMarket()
                    self._reset_protection()
                elif self._take_price is not None and low <= self._take_price:
                    self.BuyMarket()
                    self._reset_protection()

        # Process indicators manually
        fast_result = process_float(self._fast_ma, close, candle.OpenTime, True)

        slow_result = process_float(self._slow_ma, close, candle.OpenTime, True)

        if fast_result.IsEmpty or slow_result.IsEmpty:
            return

        fast_val = float(fast_result)
        slow_val = float(slow_result)

        prev_fast = self._fast_prev
        prev_slow = self._slow_prev
        prev_fast_prev = self._fast_prev_prev
        prev_slow_prev = self._slow_prev_prev

        self._fast_prev_prev = prev_fast
        self._slow_prev_prev = prev_slow
        self._fast_prev = fast_val
        self._slow_prev = slow_val

        cross_up = False
        cross_down = False
        min_dist = self._min_cross_dist.Value

        # Confirmed mode: use prev-prev vs prev
        if prev_fast is not None and prev_slow is not None and prev_fast_prev is not None and prev_slow_prev is not None:
            diff = prev_fast - prev_slow
            cross_up = prev_fast_prev < prev_slow_prev and prev_fast > prev_slow and diff >= min_dist
            cross_down = prev_fast_prev > prev_slow_prev and prev_fast < prev_slow and -diff >= min_dist

        buy_signal = cross_up
        sell_signal = cross_down

        # Stop and reverse
        if self._stop_and_reverse.Value and self.Position != 0:
            if (self._last_trade == 1 and sell_signal) or (self._last_trade == -1 and buy_signal):
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._reset_protection()

        if self.Position != 0:
            return

        if buy_signal:
            self.BuyMarket()
            self._set_protection(close, True)
            self._last_trade = 1
        elif sell_signal:
            self.SellMarket()
            self._set_protection(close, False)
            self._last_trade = -1

    def _set_protection(self, entry, is_long):
        self._entry_price = entry
        sl = self._sl.Value
        tp = self._tp.Value
        self._stop_price = (entry - sl if is_long else entry + sl) if sl > 0 else None
        self._take_price = (entry + tp if is_long else entry - tp) if tp > 0 else None

    def _reset_protection(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def _update_trailing(self, close):
        trail = self._trailing_stop.Value
        step = self._trailing_step.Value
        if trail <= 0 or self._entry_price is None:
            return

        activation = trail + step
        if self.Position > 0:
            if close - self._entry_price > activation:
                new_stop = close - trail
                if self._stop_price is None or new_stop > self._stop_price:
                    self._stop_price = new_stop
        elif self.Position < 0:
            if self._entry_price - close > activation:
                new_stop = close + trail
                if self._stop_price is None or new_stop < self._stop_price:
                    self._stop_price = new_stop

    def CreateClone(self):
        return universal_ma_cross_strategy()
