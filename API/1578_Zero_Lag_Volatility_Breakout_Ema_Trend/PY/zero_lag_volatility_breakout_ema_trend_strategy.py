import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zero_lag_volatility_breakout_ema_trend_strategy(Strategy):
    def __init__(self):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Base EMA length", "Indicators")
        self._std_multiplier = self.Param("StdMultiplier", 2.0) \
            .SetDisplay("Std Mult", "Standard deviation multiplier", "Indicators")
        self._use_binary = self.Param("UseBinary", True) \
            .SetDisplay("Use Binary", "Hold until opposite signal", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._difs = []
        self._prev_ema = 0.0
        self._prev_dif = 0.0
        self._has_prev = False

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def std_multiplier(self):
        return self._std_multiplier.Value

    @property
    def use_binary(self):
        return self._use_binary.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).OnReseted()
        self._difs = []
        self._prev_ema = 0.0
        self._prev_dif = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(zero_lag_volatility_breakout_ema_trend_strategy, self).OnStarted2(time)
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
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        h_jumper = max(close, ema_val)
        l_jumper = min(close, ema_val)
        dif = 0.0 if l_jumper == 0 else (h_jumper / l_jumper) - 1.0
        self._difs.append(dif)
        el = int(self.ema_length)
        while len(self._difs) > el + 10:
            self._difs.pop(0)
        if len(self._difs) < 20:
            self._prev_ema = ema_val
            self._prev_dif = dif
            self._has_prev = True
            return
        lookback = min(len(self._difs), el)
        recent = self._difs[-lookback:]
        mean = sum(recent) / lookback
        sum_sq = sum((v - mean) * (v - mean) for v in recent)
        std = Math.Sqrt(float(sum_sq / lookback))
        bbu = mean + std * float(self.std_multiplier)
        bbm = mean
        if not self._has_prev:
            self._prev_dif = dif
            self._prev_ema = ema_val
            self._has_prev = True
            return
        sig_enter = self._prev_dif <= bbu and dif > bbu
        sig_exit = dif < bbm
        enter_long = sig_enter and ema_val > self._prev_ema
        enter_short = sig_enter and ema_val < self._prev_ema
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
        self._prev_ema = ema_val

    def CreateClone(self):
        return zero_lag_volatility_breakout_ema_trend_strategy()
