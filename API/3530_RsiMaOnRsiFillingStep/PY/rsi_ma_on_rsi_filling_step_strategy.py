import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_ma_on_rsi_filling_step_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_on_rsi_filling_step_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._ma_period = self.Param("MaPeriod", 20)

        self._rsi_history = []
        self._prev_rsi = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(rsi_ma_on_rsi_filling_step_strategy, self).OnReseted()
        self._rsi_history = []
        self._prev_rsi = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(rsi_ma_on_rsi_filling_step_strategy, self).OnStarted(time)
        self._rsi_history = []
        self._prev_rsi = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        # Accumulate RSI values for moving average
        self._rsi_history.append(rsi_val)
        ma_len = self.MaPeriod
        while len(self._rsi_history) > ma_len:
            self._rsi_history.pop(0)

        # Need enough RSI values to compute the MA
        if len(self._rsi_history) < ma_len:
            self._prev_rsi = rsi_val
            return

        # Calculate SMA of RSI
        signal_val = sum(self._rsi_history) / ma_len

        if self._has_prev:
            cross_up = self._prev_rsi < self._prev_signal and rsi_val > signal_val
            cross_down = self._prev_rsi > self._prev_signal and rsi_val < signal_val

            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._prev_signal = signal_val
        self._has_prev = True

    def CreateClone(self):
        return rsi_ma_on_rsi_filling_step_strategy()
