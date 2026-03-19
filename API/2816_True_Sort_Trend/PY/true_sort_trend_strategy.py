import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageDirectionalIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class true_sort_trend_strategy(Strategy):
    """Five EMA alignment with ADX confirmation, SL/TP and trailing stop."""
    def __init__(self):
        super(true_sort_trend_strategy, self).__init__()
        self._fast_len = self.Param("FastEmaLength", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fastest EMA", "MAs")
        self._second_len = self.Param("SecondEmaLength", 20).SetGreaterThanZero().SetDisplay("2nd EMA", "Second EMA", "MAs")
        self._third_len = self.Param("ThirdEmaLength", 30).SetGreaterThanZero().SetDisplay("3rd EMA", "Third EMA", "MAs")
        self._fourth_len = self.Param("FourthEmaLength", 40).SetGreaterThanZero().SetDisplay("4th EMA", "Fourth EMA", "MAs")
        self._slow_len = self.Param("SlowEmaLength", 50).SetGreaterThanZero().SetDisplay("Slow EMA", "Slowest EMA", "MAs")
        self._adx_period = self.Param("AdxPeriod", 24).SetGreaterThanZero().SetDisplay("ADX Period", "ADX averaging", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 10.0).SetGreaterThanZero().SetDisplay("ADX Threshold", "Min ADX for trend", "Indicators")
        self._sl_dist = self.Param("StopLossDistance", 500.0).SetDisplay("Stop Loss", "SL distance", "Risk")
        self._tp_dist = self.Param("TakeProfitDistance", 1500.0).SetDisplay("Take Profit", "TP distance", "Risk")
        self._trail_dist = self.Param("TrailingStopDistance", 300.0).SetDisplay("Trailing Stop", "Trail distance", "Risk")
        self._trail_step = self.Param("TrailingStepDistance", 100.0).SetDisplay("Trailing Step", "Trail step", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(true_sort_trend_strategy, self).OnReseted()
        self._prev = [0, 0, 0, 0, 0]
        self._has_prev = False
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._pos_dir = 0

    def OnStarted(self, time):
        super(true_sort_trend_strategy, self).OnStarted(time)
        self._prev = [0, 0, 0, 0, 0]
        self._has_prev = False
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._pos_dir = 0

        self._ema1 = ExponentialMovingAverage()
        self._ema1.Length = self._fast_len.Value
        self._ema2 = ExponentialMovingAverage()
        self._ema2.Length = self._second_len.Value
        self._ema3 = ExponentialMovingAverage()
        self._ema3.Length = self._third_len.Value
        self._ema4 = ExponentialMovingAverage()
        self._ema4.Length = self._fourth_len.Value
        self._ema5 = ExponentialMovingAverage()
        self._ema5.Length = self._slow_len.Value
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self._adx_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._ema1, self._ema2, self._ema3, self._ema4, self._ema5, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, v1, v2, v3, v4, v5):
        if candle.State != CandleStates.Finished:
            return

        # Process ADX manually
        inp = DecimalIndicatorValue(self._adx, candle.ClosePrice)
        inp.IsFinal = True
        adx_result = self._adx.Process(inp)
        if not self._adx.IsFormed:
            return
        adx_val = float(adx_result.ToDecimal()) if not adx_result.IsEmpty else 0

        vals = [float(v1), float(v2), float(v3), float(v4), float(v5)]
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if not self._has_prev:
            self._prev = vals[:]
            self._has_prev = True
            return

        asc_cur = vals[0] > vals[1] and vals[1] > vals[2] and vals[2] > vals[3] and vals[3] > vals[4]
        desc_cur = vals[0] < vals[1] and vals[1] < vals[2] and vals[2] < vals[3] and vals[3] < vals[4]
        asc_prev = self._prev[0] > self._prev[1] and self._prev[1] > self._prev[2] and self._prev[2] > self._prev[3] and self._prev[3] > self._prev[4]
        desc_prev = self._prev[0] < self._prev[1] and self._prev[1] < self._prev[2] and self._prev[2] < self._prev[3] and self._prev[3] < self._prev[4]

        long_signal = adx_val > self._adx_threshold.Value and asc_cur and asc_prev
        short_signal = adx_val > self._adx_threshold.Value and desc_cur and desc_prev

        sl = self._sl_dist.Value
        tp = self._tp_dist.Value
        trail = self._trail_dist.Value
        trail_s = self._trail_step.Value

        # Exit management
        if self.Position > 0:
            if self._highest == 0:
                self._highest = high
            else:
                self._highest = max(self._highest, high)
            should_exit = False
            if sl > 0 and low <= self._entry_price - sl:
                should_exit = True
            if not should_exit and tp > 0 and high >= self._entry_price + tp:
                should_exit = True
            if not should_exit and trail > 0:
                activation = trail + max(0, trail_s)
                if self._highest - self._entry_price >= activation:
                    trail_level = self._highest - trail
                    if close <= trail_level:
                        should_exit = True
            if not should_exit and not asc_cur:
                should_exit = True
            if should_exit:
                self.SellMarket()
                self._reset_trade()
        elif self.Position < 0:
            if self._lowest == 0:
                self._lowest = low
            else:
                self._lowest = min(self._lowest, low)
            should_exit = False
            if sl > 0 and high >= self._entry_price + sl:
                should_exit = True
            if not should_exit and tp > 0 and low <= self._entry_price - tp:
                should_exit = True
            if not should_exit and trail > 0:
                activation = trail + max(0, trail_s)
                if self._entry_price - self._lowest >= activation:
                    trail_level = self._lowest + trail
                    if close >= trail_level:
                        should_exit = True
            if not should_exit and not desc_cur:
                should_exit = True
            if should_exit:
                self.BuyMarket()
                self._reset_trade()

        # Entries
        if long_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._highest = high
            self._lowest = low
            self._pos_dir = 1
        elif short_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._highest = high
            self._lowest = low
            self._pos_dir = -1

        self._prev = vals[:]

    def _reset_trade(self):
        self._entry_price = 0
        self._highest = 0
        self._lowest = 0
        self._pos_dir = 0

    def CreateClone(self):
        return true_sort_trend_strategy()
