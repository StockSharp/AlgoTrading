import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class crunchsters_turtle_and_trend_system_strategy(Strategy):
    """
    Crunchster's Turtle and Trend System strategy.
    The trend signal is the difference between a FastEmaPeriod EMA and an EMA five times slower: crossing above zero goes long and
    crossing below zero goes short (TrendEnabled). The breakout signal goes long on a close above the highest high of the previous
    BreakoutPeriod candles and short on a close below the lowest low (BreakoutEnabled). An opposite signal reverses the position.
    A long closes below the lowest low of the previous TrailPeriod candles, a short above the highest high, and a stop
    StopAtrMultiple ATRs from the entry limits the loss.
    """

    _atr_length = 14
    _slow_ema_factor = 5

    def __init__(self):
        super(crunchsters_turtle_and_trend_system_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ema_period = self.Param("FastEmaPeriod", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period; the slow EMA is five times longer", "Trend")
        self._breakout_period = self.Param("BreakoutPeriod", 20).SetGreaterThanZero().SetDisplay("Breakout Period", "Donchian breakout period", "Breakout")
        self._trail_period = self.Param("TrailPeriod", 1000).SetGreaterThanZero().SetDisplay("Trail Period", "Trailing Donchian exit period", "Risk")
        self._stop_atr_multiple = self.Param("StopAtrMultiple", 20.0).SetNotNegative().SetDisplay("Stop ATR Multiple", "ATR multiple for the stop", "Risk")
        self._order_percent = self.Param("OrderPercent", 10.0).SetGreaterThanZero().SetDisplay("Order %", "Percent of equity per order", "Risk")
        self._trend_enabled = self.Param("TrendEnabled", True).SetDisplay("Trend Enabled", "Trade the EMA trend signal", "Trend")
        self._breakout_enabled = self.Param("BreakoutEnabled", False).SetDisplay("Breakout Enabled", "Trade the Donchian breakout signal", "Breakout")
        self._breakout_high = None
        self._breakout_low = None
        self._trail_high = None
        self._trail_low = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_diff = None
        self._prev_breakout_high = None
        self._prev_breakout_low = None
        self._prev_trail_high = None
        self._prev_trail_low = None
        self._stop_price = None

    def OnReseted(self):
        super(crunchsters_turtle_and_trend_system_strategy, self).OnReseted()
        self._reset_state()
        self._breakout_high = None
        self._breakout_low = None
        self._trail_high = None
        self._trail_low = None

    def OnStarted2(self, time):
        super(crunchsters_turtle_and_trend_system_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._fast_ema_period.Value * self._slow_ema_factor
        atr = AverageTrueRange()
        atr.Length = self._atr_length
        self._breakout_high = Highest()
        self._breakout_high.Length = self._breakout_period.Value
        self._breakout_low = Lowest()
        self._breakout_low.Length = self._breakout_period.Value
        self._trail_high = Highest()
        self._trail_high.Length = self._trail_period.Value
        self._trail_low = Lowest()
        self._trail_low.Length = self._trail_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    @staticmethod
    def _track(indicator, value, time):
        result = process_value(indicator, value, time, True)
        return result.GetValue[Decimal](None) if result.IsFormed else None

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Channels are measured on the candles before this one.
        breakout_high = self._prev_breakout_high
        breakout_low = self._prev_breakout_low
        trail_high = self._prev_trail_high
        trail_low = self._prev_trail_low

        self._prev_breakout_high = self._track(self._breakout_high, candle.HighPrice, candle.OpenTime)
        self._prev_breakout_low = self._track(self._breakout_low, candle.LowPrice, candle.OpenTime)
        self._prev_trail_high = self._track(self._trail_high, candle.HighPrice, candle.OpenTime)
        self._prev_trail_low = self._track(self._trail_low, candle.LowPrice, candle.OpenTime)

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        diff = fast_value.GetValue[Decimal](None) - slow_value.GetValue[Decimal](None)
        prev_diff = self._prev_diff
        self._prev_diff = diff

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        long_signal = False
        short_signal = False

        if self._trend_enabled.Value and prev_diff is not None:
            long_signal = long_signal or (prev_diff <= 0 and diff > 0)
            short_signal = short_signal or (prev_diff >= 0 and diff < 0)

        if self._breakout_enabled.Value:
            long_signal = long_signal or (breakout_high is not None and close > breakout_high)
            short_signal = short_signal or (breakout_low is not None and close < breakout_low)

        multiple = Decimal(self._stop_atr_multiple.Value)
        stop_distance = atr_value.GetValue[Decimal](None) * multiple if atr_value.IsFormed and multiple > 0 else None

        if long_signal and not short_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance if stop_distance is not None else None
            return

        if short_signal and not long_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance if stop_distance is not None else None
            return

        if self.Position > 0:
            if (self._stop_price is not None and candle.LowPrice <= self._stop_price) or (trail_low is not None and close < trail_low):
                self.SellMarket(self.Position)
                self._stop_price = None
        elif self.Position < 0:
            if (self._stop_price is not None and candle.HighPrice >= self._stop_price) or (trail_high is not None and close > trail_high):
                self.BuyMarket(-self.Position)
                self._stop_price = None

    def CreateClone(self):
        return crunchsters_turtle_and_trend_system_strategy()
