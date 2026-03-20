import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class cs2011_strategy(Strategy):
    """MACD based reversal strategy reacting to zero-line crosses and signal-line extremes."""

    def __init__(self):
        super(cs2011_strategy, self).__init__()

        self._target_volume = self.Param("TargetVolume", 1.0) \
            .SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2200) \
            .SetDisplay("Take Profit (points)", "Take-profit distance in price points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 0) \
            .SetDisplay("Stop Loss (points)", "Stop-loss distance in price points", "Risk")
        self._fast_ema_period = self.Param("FastEmaPeriod", 30) \
            .SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50) \
            .SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 36) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source timeframe for MACD", "General")

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._signal_prev1 = None
        self._signal_prev2 = None
        self._signal_prev3 = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TargetVolume(self):
        return self._target_volume.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    def OnReseted(self):
        super(cs2011_strategy, self).OnReseted()
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._signal_prev1 = None
        self._signal_prev2 = None
        self._signal_prev3 = None

    def OnStarted(self, time):
        super(cs2011_strategy, self).OnStarted(time)

        self.Volume = float(self.TargetVolume)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastEmaPeriod
        macd.Macd.LongMa.Length = self.SlowEmaPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, indicator_value):
        if candle.State != CandleStates.Finished:
            return

        macd_raw = indicator_value.Macd
        signal_raw = indicator_value.Signal

        if macd_raw is None or signal_raw is None:
            return

        macd = float(macd_raw)
        signal = float(signal_raw)

        prev_macd1 = self._macd_prev1
        prev_macd2 = self._macd_prev2
        prev_signal1 = self._signal_prev1
        prev_signal2 = self._signal_prev2
        prev_signal3 = self._signal_prev3

        up_signal = False
        down_signal = False

        if prev_macd1 is not None and prev_macd2 is not None:
            if prev_macd1 > 0 and prev_macd2 < 0:
                down_signal = True
            if prev_macd1 < 0 and prev_macd2 > 0:
                up_signal = True

        if prev_macd2 is not None and prev_signal1 is not None and \
                prev_signal2 is not None and prev_signal3 is not None:
            if prev_macd2 < 0 and prev_signal1 < prev_signal2 and prev_signal2 > prev_signal3:
                down_signal = True
            if prev_macd2 > 0 and prev_signal1 > prev_signal2 and prev_signal2 < prev_signal3:
                up_signal = True

        if up_signal or down_signal:
            self._execute_signals(up_signal, down_signal)

        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = macd
        self._signal_prev3 = self._signal_prev2
        self._signal_prev2 = self._signal_prev1
        self._signal_prev1 = signal

    def _execute_signals(self, up_signal, down_signal):
        target = float(self.TargetVolume)

        if up_signal:
            target_position = target
            difference = target_position - self.Position
            if difference > 0:
                self.BuyMarket(difference)

        if down_signal:
            target_position = -target
            difference = target_position - self.Position
            if difference < 0:
                self.SellMarket(abs(difference))

    def CreateClone(self):
        return cs2011_strategy()
