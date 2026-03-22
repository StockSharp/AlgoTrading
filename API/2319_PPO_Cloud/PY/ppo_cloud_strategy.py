import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import PPO, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ppo_cloud_strategy(Strategy):
    def __init__(self):
        super(ppo_cloud_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA length", "PPO")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA length", "PPO")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal EMA length", "PPO")
        self._prev_ppo = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._ppo = None
        self._signal_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def signal_period(self):
        return self._signal_period.Value

    def OnReseted(self):
        super(ppo_cloud_strategy, self).OnReseted()
        self._prev_ppo = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._ppo = None
        self._signal_ema = None

    def OnStarted(self, time):
        super(ppo_cloud_strategy, self).OnStarted(time)
        self._prev_ppo = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._ppo = PPO()
        self._ppo.ShortPeriod = self.fast_period
        self._ppo.LongPeriod = self.slow_period
        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self.signal_period
        self.Indicators.Add(self._signal_ema)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ppo, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ppo)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ppo_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._ppo.IsFormed:
            return
        ppo_value = float(ppo_value)
        sig_input = DecimalIndicatorValue(self._signal_ema, ppo_value, candle.CloseTime)
        sig_input.IsFinal = True
        sig_result = self._signal_ema.Process(sig_input)
        if not self._signal_ema.IsFormed:
            return
        signal_value = float(sig_result)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ppo = ppo_value
            self._prev_signal = signal_value
            self._has_prev = True
            return
        if self._has_prev:
            cross_up = self._prev_ppo <= self._prev_signal and ppo_value > signal_value
            cross_down = self._prev_ppo >= self._prev_signal and ppo_value < signal_value
            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()
        self._prev_ppo = ppo_value
        self._prev_signal = signal_value
        self._has_prev = True

    def CreateClone(self):
        return ppo_cloud_strategy()
