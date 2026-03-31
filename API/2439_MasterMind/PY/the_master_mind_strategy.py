import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class the_master_mind_strategy(Strategy):
    def __init__(self):
        super(the_master_mind_strategy, self).__init__()

        self._stoch_length = self.Param("StochasticLength", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._wpr = None
        self._prev_d = None
        self._last_signal = 0

    @property
    def StochasticLength(self):
        return self._stoch_length.Value

    @StochasticLength.setter
    def StochasticLength(self, value):
        self._stoch_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(the_master_mind_strategy, self).OnStarted2(time)

        self._prev_d = None
        self._last_signal = 0

        self._wpr = WilliamsR()
        self._wpr.Length = self.StochasticLength

        stoch = StochasticOscillator()
        stoch.K.Length = self.StochasticLength
        stoch.D.Length = 3

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stoch, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        wpr_input = CandleIndicatorValue(self._wpr, candle)
        wpr_input.IsFinal = True
        wpr_result = self._wpr.Process(wpr_input)
        if not self._wpr.IsFormed:
            return

        wpr = float(wpr_result)
        d = float(stoch_value.D)
        k = float(stoch_value.K)

        if self._prev_d is not None:
            buy_signal = self._prev_d >= 20.0 and d < 20.0 and wpr < -85.0
            sell_signal = self._prev_d <= 80.0 and d > 80.0 and wpr > -15.0

            if buy_signal and self.Position <= 0:
                self.BuyMarket()
                self._last_signal = 1
            elif sell_signal and self.Position >= 0:
                self.SellMarket()
                self._last_signal = -1

        self._prev_d = d

    def OnReseted(self):
        super(the_master_mind_strategy, self).OnReseted()
        self._wpr = None
        self._prev_d = None
        self._last_signal = 0

    def CreateClone(self):
        return the_master_mind_strategy()
