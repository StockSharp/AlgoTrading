import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class adaptive_fibonacci_pullback_strategy(Strategy):
    def __init__(self):
        super(adaptive_fibonacci_pullback_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("SuperTrend ATR Length", "ATR period for SuperTrend", "SuperTrend")
        self._factor1 = self.Param("Factor1", 0.618) \
            .SetDisplay("Factor 1", "Weak Fibonacci factor", "SuperTrend")
        self._factor2 = self.Param("Factor2", 1.618) \
            .SetDisplay("Factor 2", "Golden Ratio factor", "SuperTrend")
        self._factor3 = self.Param("Factor3", 2.618) \
            .SetDisplay("Factor 3", "Extended Fibonacci factor", "SuperTrend")
        self._smooth_length = self.Param("SmoothLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Smoothing Length", "EMA length for SuperTrend average", "SuperTrend")
        self._ama_length = self.Param("AmaLength", 55) \
            .SetGreaterThanZero() \
            .SetDisplay("AMA Length", "Length for AMA midline", "AMA")
        self._rsi_length = self.Param("RsiLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_buy = self.Param("RsiBuy", 50.0) \
            .SetDisplay("RSI Buy Threshold", "RSI must be above for long", "RSI")
        self._rsi_sell = self.Param("RsiSell", 50.0) \
            .SetDisplay("RSI Sell Threshold", "RSI must be below for short", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_close = 0.0
        self._prev_smooth = 0.0
        self._is_first = True
        self._cooldown_remaining = 0
        self._smooth_val = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_fibonacci_pullback_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_smooth = 0.0
        self._is_first = True
        self._cooldown_remaining = 0
        self._smooth_val = 0.0

    def OnStarted(self, time):
        super(adaptive_fibonacci_pullback_strategy, self).OnStarted(time)
        st1 = SuperTrend()
        st1.Length = self._atr_period.Value
        st1.Multiplier = self._factor1.Value
        st2 = SuperTrend()
        st2.Length = self._atr_period.Value
        st2.Multiplier = self._factor2.Value
        st3 = SuperTrend()
        st3.Length = self._atr_period.Value
        st3.Multiplier = self._factor3.Value
        ama_mid = ExponentialMovingAverage()
        ama_mid.Length = self._ama_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(st1, st2, st3, ama_mid, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, st1_val, st2_val, st3_val, mid_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        st1_v = float(st1_val)
        st2_v = float(st2_val)
        st3_v = float(st3_val)
        mid = float(mid_val)
        rsi_v = float(rsi_val)
        avg = (st1_v + st2_v + st3_v) / 3.0
        alpha = 2.0 / (self._smooth_length.Value + 1.0)
        if self._is_first:
            self._smooth_val = avg
        else:
            self._smooth_val = alpha * avg + (1.0 - alpha) * self._smooth_val
        smooth = self._smooth_val
        close = float(candle.ClosePrice)
        if self._is_first:
            self._prev_close = close
            self._prev_smooth = smooth
            self._is_first = False
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_smooth = smooth
            return
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        base_long = low < avg and close > smooth and self._prev_close > mid
        base_short = high > avg and close < smooth and self._prev_close < mid
        long_entry = base_long and close > mid and rsi_v > float(self._rsi_buy.Value)
        short_entry = base_short and close < mid and rsi_v < float(self._rsi_sell.Value)
        if long_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif short_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        long_exit = self._prev_close > self._prev_smooth and close <= smooth and self.Position > 0
        short_exit = self._prev_close < self._prev_smooth and close >= smooth and self.Position < 0
        if long_exit:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif short_exit:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_close = close
        self._prev_smooth = smooth

    def CreateClone(self):
        return adaptive_fibonacci_pullback_strategy()
