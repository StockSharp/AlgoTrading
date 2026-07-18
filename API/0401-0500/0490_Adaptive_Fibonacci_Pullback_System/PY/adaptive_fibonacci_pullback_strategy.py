import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (SuperTrend, ExponentialMovingAverage,
                                         RelativeStrengthIndex,
                                         IndicatorHelper, IIndicator)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class adaptive_fibonacci_pullback_strategy(Strategy):
    """Adaptive Fibonacci Pullback Strategy."""

    def __init__(self):
        super(adaptive_fibonacci_pullback_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 8) \
            .SetDisplay("SuperTrend ATR Length", "ATR period for SuperTrend", "SuperTrend")
        self._factor1 = self.Param("Factor1", 0.618) \
            .SetDisplay("Factor 1", "Weak Fibonacci factor", "SuperTrend")
        self._factor2 = self.Param("Factor2", 1.618) \
            .SetDisplay("Factor 2", "Golden Ratio factor", "SuperTrend")
        self._factor3 = self.Param("Factor3", 2.618) \
            .SetDisplay("Factor 3", "Extended Fibonacci factor", "SuperTrend")
        self._smooth_length = self.Param("SmoothLength", 21) \
            .SetDisplay("Smoothing Length", "EMA length for SuperTrend average", "SuperTrend")
        self._ama_length = self.Param("AmaLength", 55) \
            .SetDisplay("AMA Length", "Length for AMA midline", "AMA")
        self._rsi_length = self.Param("RsiLength", 7) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_buy = self.Param("RsiBuy", 50.0) \
            .SetDisplay("RSI Buy Threshold", "RSI must be above for long", "RSI")
        self._rsi_sell = self.Param("RsiSell", 50.0) \
            .SetDisplay("RSI Sell Threshold", "RSI must be below for short", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._st1 = None
        self._st2 = None
        self._st3 = None
        self._st_smooth = None
        self._ama_mid = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_smooth = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_fibonacci_pullback_strategy, self).OnReseted()
        self._st1 = None
        self._st2 = None
        self._st3 = None
        self._st_smooth = None
        self._ama_mid = None
        self._rsi = None
        self._prev_close = 0.0
        self._prev_smooth = 0.0
        self._is_first = True
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adaptive_fibonacci_pullback_strategy, self).OnStarted2(time)

        self._st1 = SuperTrend()
        self._st1.Length = int(self._atr_period.Value)
        self._st1.Multiplier = self._factor1.Value
        self._st2 = SuperTrend()
        self._st2.Length = int(self._atr_period.Value)
        self._st2.Multiplier = self._factor2.Value
        self._st3 = SuperTrend()
        self._st3.Length = int(self._atr_period.Value)
        self._st3.Multiplier = self._factor3.Value
        self._st_smooth = ExponentialMovingAverage()
        self._st_smooth.Length = int(self._smooth_length.Value)
        self._ama_mid = ExponentialMovingAverage()
        self._ama_mid.Length = int(self._ama_length.Value)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        indicators = Array[IIndicator]([self._st1, self._st2, self._st3, self._ama_mid, self._rsi])

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(indicators, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if values[0].IsEmpty or values[1].IsEmpty or values[2].IsEmpty or values[3].IsEmpty or values[4].IsEmpty:
            return

        st1_v = float(IndicatorHelper.ToDecimal(values[0]))
        st2_v = float(IndicatorHelper.ToDecimal(values[1]))
        st3_v = float(IndicatorHelper.ToDecimal(values[2]))
        mid = float(IndicatorHelper.ToDecimal(values[3]))
        rsi_v = float(IndicatorHelper.ToDecimal(values[4]))

        avg = (st1_v + st2_v + st3_v) / 3.0

        smooth_result = process_float(self._st_smooth, avg, candle.ServerTime, True)
        smooth = float(IndicatorHelper.ToDecimal(smooth_result))

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

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
        rsi_buy = float(self._rsi_buy.Value)
        rsi_sell = float(self._rsi_sell.Value)

        base_long = low < avg and close > smooth and self._prev_close > mid
        base_short = high > avg and close < smooth and self._prev_close < mid

        long_entry = base_long and close > mid and rsi_v > rsi_buy
        short_entry = base_short and close < mid and rsi_v < rsi_sell

        if long_entry and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif short_entry and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        # Exit conditions
        long_exit = self._prev_close > self._prev_smooth and close <= smooth and self.Position > 0
        short_exit = self._prev_close < self._prev_smooth and close >= smooth and self.Position < 0

        if long_exit:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif short_exit:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_close = close
        self._prev_smooth = smooth

    def CreateClone(self):
        return adaptive_fibonacci_pullback_strategy()
