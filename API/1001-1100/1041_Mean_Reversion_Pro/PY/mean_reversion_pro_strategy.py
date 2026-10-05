import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

LONG_ONLY = "long only"
SHORT_ONLY = "short only"


class mean_reversion_pro_strategy(Strategy):
    """
    Mean Reversion Pro strategy.
    When flat, a long opens on a close below the fast SMA, inside the lowest 20% of the candle range and above the slow SMA;
    a short opens on a close above the fast SMA, inside the highest 20% of the range (above the 80% level) and below the slow SMA.
    A long closes when the close crosses above the fast SMA and a short when it crosses below. Direction selects
    "Long only", "Short only" or "Both".
    """

    def __init__(self):
        super(mean_reversion_pro_strategy, self).__init__()
        self._fast_sma = self.Param("FastSma", 5).SetGreaterThanZero().SetDisplay("Fast SMA", "Fast SMA length", "Indicators")
        self._slow_sma = self.Param("SlowSma", 100).SetGreaterThanZero().SetDisplay("Slow SMA", "Slow SMA length", "Indicators")
        self._direction = self.Param("Direction", "Long only").SetDisplay("Direction", "Allowed trade direction: Long only, Short only or Both", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_fast = None

    def OnReseted(self):
        super(mean_reversion_pro_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(mean_reversion_pro_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._fast_sma.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_sma.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        prev_close = self._prev_close
        prev_fast = self._prev_fast
        self._prev_close = close
        self._prev_fast = fast

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        has_prev = prev_close is not None and prev_fast is not None
        crossed_up = has_prev and prev_close <= prev_fast and close > fast
        crossed_down = has_prev and prev_close >= prev_fast and close < fast

        if self.Position > 0:
            if crossed_up:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if crossed_down:
                self.BuyMarket(-self.Position)
            return

        range_ = candle.HighPrice - candle.LowPrice
        low_level = candle.LowPrice + Decimal(0.2) * range_
        high_level = candle.LowPrice + Decimal(0.8) * range_

        direction = (self._direction.Value or "").strip().lower()
        allow_long = direction != SHORT_ONLY
        allow_short = direction != LONG_ONLY

        if allow_long and close < fast and close < low_level and close > slow:
            self.BuyMarket(self.Volume)
        elif allow_short and close > fast and close > high_level and close < slow:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return mean_reversion_pro_strategy()
