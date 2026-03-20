import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zero_lag_volatility_breakout_ema_trend_strategy(Strategy):
    def __init__(self):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).__init__()
        self._prev_ema = 0.0
        self._prev_dif = 0.0
        self._has_prev = False

    def OnReseted(self):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_dif = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        h_jumper = max(candle.ClosePrice, ema_value)
        l_jumper = min(candle.ClosePrice, ema_value)
        dif = (0 if l_jumper == 0 else (h_jumper / l_jumper) - 1)
        self._difs.append(dif)
        if len(self._difs) > self.ema_length + 10:
            self._difs.pop(0)
        if len(self._difs) < 20:
            self._prev_ema = ema_value
            self._prev_dif = dif
            self._has_prev = True
            return
        # Compute Bollinger-like bands on dif values
        lookback = min(len(self._difs), self.ema_length)
        recent = self._difs.Skip(len(self._difs) - lookback).ToList()
        mean = recent.Average()
        sum_sq = recent.Sum(v => (v - mean) * (v - mean))
        std = float(Math.Sqrt((double)(sum_sq / lookback)))
        bbu = mean + std * self.std_multiplier
        bbm = mean
        if not self._has_prev:
            self._prev_dif = dif
            self._prev_ema = ema_value
            self._has_prev = True
            return
        sig_enter = self._prev_dif <= bbu and dif > bbu
        sig_exit = dif < bbm
        enter_long = sig_enter and ema_value > self._prev_ema
        enter_short = sig_enter and ema_value < self._prev_ema
        if enter_long and self.Position <= 0:
            self.BuyMarket()
        elif enter_short and self.Position >= 0:
            self.SellMarket()
        elif not self.use_binary and sig_exit:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
        self._prev_dif = dif
        self._prev_ema = ema_value

    def CreateClone(self):
        return zero_lag_volatility_breakout_ema_trend_strategy()
