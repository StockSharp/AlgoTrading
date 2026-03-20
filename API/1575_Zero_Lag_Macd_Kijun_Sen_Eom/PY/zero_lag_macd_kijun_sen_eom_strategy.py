import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zero_lag_macd_kijun_sen_eom_strategy(Strategy):
    def __init__(self):
        super(zero_lag_macd_kijun_sen_eom_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "Slow EMA period", "Indicators")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal Length", "Signal smoothing", "Indicators")
        self._stop_pct = self.Param("StopPct", 1.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._risk_reward = self.Param("RiskReward", 1.5) \
            .SetDisplay("Risk/Reward", "Take profit ratio", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_macd = 0.0
        self._prev_signal_ema = 0.0
        self._has_prev = False

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def signal_length(self):
        return self._signal_length.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def risk_reward(self):
        return self._risk_reward.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zero_lag_macd_kijun_sen_eom_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal_ema = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(zero_lag_macd_kijun_sen_eom_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        baseline = SimpleMovingAverage()
        baseline.Length = 26
        self._prev_macd = 0.0
        self._prev_signal_ema = 0.0
        self._has_prev = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, baseline, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, baseline)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val, baseline_val):
        if candle.State != CandleStates.Finished:
            return
        macd = float(fast_val) - float(slow_val)
        if not self._has_prev:
            signal = macd
            self._prev_macd = macd
            self._prev_signal_ema = signal
            self._has_prev = True
            return
        k = 2.0 / (self.signal_length + 1)
        signal = macd * k + self._prev_signal_ema * (1.0 - k)
        macd_cross_up = self._prev_macd <= self._prev_signal_ema and macd > signal
        macd_cross_down = self._prev_macd >= self._prev_signal_ema and macd < signal
        if macd_cross_up and self.Position <= 0:
            self.BuyMarket()
        elif macd_cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_macd = macd
        self._prev_signal_ema = signal

    def CreateClone(self):
        return zero_lag_macd_kijun_sen_eom_strategy()
