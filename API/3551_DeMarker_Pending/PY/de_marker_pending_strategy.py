import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class de_marker_pending_strategy(Strategy):
    def __init__(self):
        super(de_marker_pending_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._demarker_period = self.Param("DemarkerPeriod", 14)
        self._demarker_upper = self.Param("DemarkerUpperLevel", 0.7)
        self._demarker_lower = self.Param("DemarkerLowerLevel", 0.3)

        self._prev_osc = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DemarkerPeriod(self):
        return self._demarker_period.Value

    @DemarkerPeriod.setter
    def DemarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def DemarkerUpperLevel(self):
        return self._demarker_upper.Value

    @DemarkerUpperLevel.setter
    def DemarkerUpperLevel(self, value):
        self._demarker_upper.Value = value

    @property
    def DemarkerLowerLevel(self):
        return self._demarker_lower.Value

    @DemarkerLowerLevel.setter
    def DemarkerLowerLevel(self, value):
        self._demarker_lower.Value = value

    def OnReseted(self):
        super(de_marker_pending_strategy, self).OnReseted()
        self._prev_osc = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(de_marker_pending_strategy, self).OnStarted2(time)
        self._prev_osc = 0.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.DemarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        osc_val = float(rsi_value) / 100.0
        upper = float(self.DemarkerUpperLevel)
        lower = float(self.DemarkerLowerLevel)

        if self._has_prev:
            # Cross below lower level => buy
            if self._prev_osc > lower and osc_val <= lower and self.Position <= 0:
                self.BuyMarket()
            # Cross above upper level => sell
            elif self._prev_osc < upper and osc_val >= upper and self.Position >= 0:
                self.SellMarket()

        self._prev_osc = osc_val
        self._has_prev = True

    def CreateClone(self):
        return de_marker_pending_strategy()
