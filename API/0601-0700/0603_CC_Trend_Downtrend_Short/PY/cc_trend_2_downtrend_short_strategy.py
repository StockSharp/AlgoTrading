import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

FAST_EMA_LENGTH = 21
SLOW_EMA_LENGTH = 55
TREND_EMA_LENGTH = 200
FIB_LEVEL = 0.236


class cc_trend_2_downtrend_short_strategy(Strategy):
    """
    CC Trend Strategy 2 Downtrend Short.
    Short only. The Fibonacci range spans the lowest low and the highest high of the last FibLength candles, and its 0.236 level lies
    23.6% of the range below the high. A short opens when the previous close is below the Fibonacci high and EMA21 is below EMA55.
    It closes when the close crosses above EMA200 while the trade is not losing, or when the previous close is above the 0.236 level
    and there is no new short signal.
    """

    def __init__(self):
        super(cc_trend_2_downtrend_short_strategy, self).__init__()
        self._fib_length = self.Param("FibLength", 100).SetGreaterThanZero().SetDisplay("Fib Length", "Candles of the Fibonacci range", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_trend_ema = None
        self._entry_price = Decimal(0)

    def OnReseted(self):
        super(cc_trend_2_downtrend_short_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cc_trend_2_downtrend_short_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = FAST_EMA_LENGTH
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = SLOW_EMA_LENGTH
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = TREND_EMA_LENGTH
        highest = Highest()
        highest.Length = self._fib_length.Value
        lowest = Lowest()
        lowest.Length = self._fib_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, trend_ema, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawIndicator(area, trend_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, trend_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        pc = self._prev_close
        pt = self._prev_trend_ema
        self._prev_close = close

        if not trend_value.IsFormed:
            return

        trend_ema = trend_value.GetValue[Decimal](None)
        self._prev_trend_ema = trend_ema

        if not fast_value.IsFormed or not slow_value.IsFormed or not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        if pc is None or pt is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fib_high = highest_value.GetValue[Decimal](None)
        fib_low = lowest_value.GetValue[Decimal](None)
        fib236 = fib_high - (fib_high - fib_low) * Decimal(FIB_LEVEL)
        short_signal = pc < fib_high and fast_value.GetValue[Decimal](None) < slow_value.GetValue[Decimal](None)

        if self.Position < 0:
            cross_above_trend = pc <= pt and close > trend_ema
            if (cross_above_trend and close <= self._entry_price) or (pc > fib236 and not short_signal):
                self.BuyMarket(-self.Position)
            return

        if self.Position == 0 and short_signal:
            self.SellMarket(self.Volume)
            self._entry_price = close

    def CreateClone(self):
        return cc_trend_2_downtrend_short_strategy()
