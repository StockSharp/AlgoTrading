import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class intra_bullish_profit_ping_v4_0_strategy(Strategy):
    def __init__(self):
        super(intra_bullish_profit_ping_v4_0_strategy, self).__init__()
        self._short_ema_length = self.Param("ShortEmaLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Short EMA", "Short EMA length", "EMA")
        self._long_ema_length = self.Param("LongEmaLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Long EMA", "Long EMA length", "EMA")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_short = None
        self._prev_long = None
        self._last_rsi = 0.0
        self._last_histogram = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(intra_bullish_profit_ping_v4_0_strategy, self).OnReseted()
        self._prev_short = None
        self._prev_long = None
        self._last_rsi = 0.0
        self._last_histogram = 0.0

    def OnStarted2(self, time):
        super(intra_bullish_profit_ping_v4_0_strategy, self).OnStarted2(time)
        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._short_ema_length.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._long_ema_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = 12
        self._macd.Macd.LongMa.Length = 26
        self._macd.SignalMa.Length = 9
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema_short, ema_long, self._rsi, self._macd, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_short_val, ema_long_val, rsi_val, macd_val):
        if candle.State != CandleStates.Finished:
            return
        if ema_short_val.IsEmpty or ema_long_val.IsEmpty or rsi_val.IsEmpty or macd_val.IsEmpty:
            return
        ema_s = float(ema_short_val)
        ema_l = float(ema_long_val)
        self._last_rsi = float(rsi_val)
        macd_v = macd_val.Macd
        signal_v = macd_val.Signal
        if macd_v is not None and signal_v is not None:
            self._last_histogram = float(macd_v) - float(signal_v)
        cross_up = self._prev_short is not None and self._prev_long is not None and self._prev_short <= self._prev_long and ema_s > ema_l
        cross_down = self._prev_short is not None and self._prev_long is not None and self._prev_short >= self._prev_long and ema_s < ema_l
        buy_signal = cross_up and self._last_rsi > 40.0
        sell_signal = cross_down and self._last_rsi < 60.0
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        if sell_signal and self.Position > 0:
            self.SellMarket()
        self._prev_short = ema_s
        self._prev_long = ema_l

    def CreateClone(self):
        return intra_bullish_profit_ping_v4_0_strategy()
