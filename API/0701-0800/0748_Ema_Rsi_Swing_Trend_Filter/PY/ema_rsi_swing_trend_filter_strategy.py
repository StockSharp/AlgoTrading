import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ema_rsi_swing_trend_filter_strategy(Strategy):
    """
    EMA RSI swing trend filter strategy.
    The fast EMA crossing above the slow EMA with the close above the trend EMA goes long, the bearish cross with the close below
    it goes short. With UseRsiFilter longs need RSI below RsiMaxLong and shorts RSI above RsiMinShort. With ExitOnOpposite an
    opposite EMA cross closes the position even when the entry filters reject a new trade.
    """

    def __init__(self):
        super(ema_rsi_swing_trend_filter_strategy, self).__init__()
        self._ema_fast_period = self.Param("EmaFastPeriod", 20).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._ema_slow_period = self.Param("EmaSlowPeriod", 50).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._ema_trend_period = self.Param("EmaTrendPeriod", 200).SetGreaterThanZero().SetDisplay("Trend EMA", "Trend EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._use_rsi_filter = self.Param("UseRsiFilter", True).SetDisplay("Use RSI Filter", "Use the RSI filter", "Filters")
        self._rsi_max_long = self.Param("RsiMaxLong", 70.0).SetDisplay("RSI Max Long", "RSI below which longs are allowed", "Filters")
        self._rsi_min_short = self.Param("RsiMinShort", 30.0).SetDisplay("RSI Min Short", "RSI above which shorts are allowed", "Filters")
        self._require_close_confirm = self.Param("RequireCloseConfirm", True).SetDisplay("Close Confirm", "Act only on closed candles", "Filters")
        self._exit_on_opposite = self.Param("ExitOnOpposite", True).SetDisplay("Exit On Opposite", "Close the position on an opposite EMA cross", "Exits")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None

    def OnReseted(self):
        super(ema_rsi_swing_trend_filter_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_rsi_swing_trend_filter_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._ema_fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._ema_slow_period.Value
        trend = ExponentialMovingAverage()
        trend.Length = self._ema_trend_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, trend, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawIndicator(area, trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, trend_value, rsi_value):
        # Signals are always evaluated on closed candles, which is what RequireCloseConfirm asks for.
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        trend = float(trend_value)
        rsi = float(rsi_value)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow
        close = float(candle.ClosePrice)
        use_rsi = self._use_rsi_filter.Value

        long_ok = cross_up and close > trend and (not use_rsi or rsi < float(self._rsi_max_long.Value))
        short_ok = cross_down and close < trend and (not use_rsi or rsi > float(self._rsi_min_short.Value))

        if long_ok and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_ok and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self._exit_on_opposite.Value:
            if self.Position > 0 and cross_down:
                self.SellMarket(self.Position)
            elif self.Position < 0 and cross_up:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ema_rsi_swing_trend_filter_strategy()
