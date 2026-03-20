import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class conditional_position_opener_strategy(Strategy):
    def __init__(self):
        super(conditional_position_opener_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._momentum_period = self.Param("MomentumPeriod", 20)

        self._prev_momentum = None

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

    def OnReseted(self):
        super(conditional_position_opener_strategy, self).OnReseted()
        self._prev_momentum = None

    def OnStarted(self, time):
        super(conditional_position_opener_strategy, self).OnStarted(time)
        self._prev_momentum = None

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(momentum, self._process_candle).Start()

    def _process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        mom_val = float(momentum_value)

        if self._prev_momentum is None:
            self._prev_momentum = mom_val
            return

        # Cross above 101 (positive momentum)
        cross_up = self._prev_momentum <= 101.0 and mom_val > 101.0
        # Cross below 99 (negative momentum)
        cross_down = self._prev_momentum >= 99.0 and mom_val < 99.0

        if cross_up:
            if self.Position <= 0:
                self.BuyMarket()
        elif cross_down:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_momentum = mom_val

    def CreateClone(self):
        return conditional_position_opener_strategy()
