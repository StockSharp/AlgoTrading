import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    MovingAverageConvergenceDivergenceSignal,
    AdaptiveLaguerreFilter,
    MoneyFlowIndex,
)

class trend_follower_rainbow_strategy(Strategy):
    def __init__(self):
        super(trend_follower_rainbow_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 17.0) \
            .SetDisplay("Take Profit (pts)", "Distance in price steps for take profit", "Risk Management")
        self._stop_loss_points = self.Param("StopLossPoints", 30.0) \
            .SetDisplay("Stop Loss (pts)", "Distance in price steps for stop loss", "Risk Management")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 45.0) \
            .SetDisplay("Trailing Stop (pts)", "Distance in price steps for trailing stop", "Risk Management")
        self._trading_start_hour = self.Param("TradingStartHour", 1) \
            .SetDisplay("Start Hour", "Hour when trading window opens", "Trading Schedule")
        self._trading_end_hour = self.Param("TradingEndHour", 23) \
            .SetDisplay("End Hour", "Hour when trading window closes", "Trading Schedule")
        self._fast_ema_length = self.Param("FastEmaLength", 4) \
            .SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 8) \
            .SetDisplay("Slow EMA", "Length of the slow EMA", "Indicators")
        self._macd_fast_length = self.Param("MacdFastLength", 5) \
            .SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
        self._macd_slow_length = self.Param("MacdSlowLength", 35) \
            .SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
        self._macd_signal_length = self.Param("MacdSignalLength", 5) \
            .SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self._previous_fast_ema = None
        self._previous_slow_ema = None
        self._point_value = 0.0

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def TradingStartHour(self):
        return self._trading_start_hour.Value

    @property
    def TradingEndHour(self):
        return self._trading_end_hour.Value

    @property
    def FastEmaLength(self):
        return self._fast_ema_length.Value

    @property
    def SlowEmaLength(self):
        return self._slow_ema_length.Value

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @property
    def MacdSignalLength(self):
        return self._macd_signal_length.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(trend_follower_rainbow_strategy, self).OnStarted2(time)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._point_value = float(ps) if ps is not None else 1.0
        self.Volume = float(self.OrderVolume)

        tp_pts = float(self.TakeProfitPoints)
        sl_pts = float(self.StopLossPoints)
        trailing_pts = float(self.TrailingStopPoints)

        tp = Unit(tp_pts * self._point_value, UnitTypes.Absolute) if tp_pts > 0 and self._point_value > 0 else None
        sl = Unit(sl_pts * self._point_value, UnitTypes.Absolute) if sl_pts > 0 and self._point_value > 0 else None

        if tp is not None or sl is not None:
            self.StartProtection(tp, sl, isStopTrailing=(trailing_pts > 0), useMarketOrders=True)

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.FastEmaLength
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.SlowEmaLength

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastLength
        macd.Macd.LongMa.Length = self.MacdSlowLength
        macd.SignalMa.Length = self.MacdSignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema_fast, ema_slow, macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, macd_val):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.CloseTime.Hour
        if hour <= self.TradingStartHour or hour >= self.TradingEndHour:
            self._update_prev(fast_val, slow_val)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_prev(fast_val, slow_val)
            return

        if fast_val.IsEmpty or slow_val.IsEmpty:
            self._update_prev(fast_val, slow_val)
            return

        fast_ema = float(fast_val)
        slow_ema = float(slow_val)

        if not macd_val.IsFinal:
            self._update_prev_values(fast_ema, slow_ema)
            return

        macd_main = macd_val.Macd
        macd_signal = macd_val.Signal
        if macd_main is None or macd_signal is None:
            self._update_prev_values(fast_ema, slow_ema)
            return

        macd_main = float(macd_main)
        macd_signal = float(macd_signal)

        ema_cross_up = (self._previous_fast_ema is not None and self._previous_slow_ema is not None
                        and self._previous_fast_ema < self._previous_slow_ema and fast_ema > slow_ema)
        ema_cross_down = (self._previous_fast_ema is not None and self._previous_slow_ema is not None
                          and self._previous_fast_ema > self._previous_slow_ema and fast_ema < slow_ema)

        macd_bullish = macd_main > 0 and macd_signal > 0
        macd_bearish = macd_main < 0 and macd_signal < 0

        if ema_cross_up and macd_bullish and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
        elif ema_cross_down and macd_bearish and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)

        self._previous_fast_ema = fast_ema
        self._previous_slow_ema = slow_ema

    def _update_prev(self, fast_val, slow_val):
        if not fast_val.IsEmpty:
            self._previous_fast_ema = float(fast_val)
        if not slow_val.IsEmpty:
            self._previous_slow_ema = float(slow_val)

    def _update_prev_values(self, fast_ema, slow_ema):
        self._previous_fast_ema = fast_ema
        self._previous_slow_ema = slow_ema

    def OnReseted(self):
        super(trend_follower_rainbow_strategy, self).OnReseted()
        self._previous_fast_ema = None
        self._previous_slow_ema = None

    def CreateClone(self):
        return trend_follower_rainbow_strategy()
