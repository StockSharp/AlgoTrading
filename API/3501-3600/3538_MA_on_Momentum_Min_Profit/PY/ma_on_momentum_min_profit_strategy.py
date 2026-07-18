import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class ma_on_momentum_min_profit_strategy(Strategy):
    def __init__(self):
        super(ma_on_momentum_min_profit_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._momentum_period = self.Param("MomentumPeriod", 20)
        self._ma_period = self.Param("MaPeriod", 10)

        self._mom_history = []
        self._prev_mom = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(ma_on_momentum_min_profit_strategy, self).OnReseted()
        self._mom_history = []
        self._prev_mom = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ma_on_momentum_min_profit_strategy, self).OnStarted2(time)
        self._mom_history = []
        self._prev_mom = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

        mom = Momentum()
        mom.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mom, self._process_candle).Start()

    def _process_candle(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return

        mom_val = float(mom_value)
        ma_len = self.MaPeriod

        self._mom_history.append(mom_val)
        while len(self._mom_history) > ma_len:
            self._mom_history.pop(0)

        if len(self._mom_history) < ma_len:
            self._prev_mom = mom_val
            return

        signal_val = sum(self._mom_history) / ma_len

        if self._has_prev:
            cross_up = self._prev_mom < self._prev_signal and mom_val > signal_val
            cross_down = self._prev_mom > self._prev_signal and mom_val < signal_val
            min_spread = 0.5

            if cross_up and abs(mom_val - signal_val) >= min_spread and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and abs(mom_val - signal_val) >= min_spread and self.Position >= 0:
                self.SellMarket()

        self._prev_mom = mom_val
        self._prev_signal = signal_val
        self._has_prev = True

    def CreateClone(self):
        return ma_on_momentum_min_profit_strategy()
