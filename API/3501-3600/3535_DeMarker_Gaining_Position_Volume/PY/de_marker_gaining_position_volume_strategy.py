import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class de_marker_gaining_position_volume_strategy(Strategy):
    def __init__(self):
        super(de_marker_gaining_position_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._demarker_period = self.Param("DeMarkerPeriod", 14)
        self._upper_level = self.Param("UpperLevel", 0.7)
        self._lower_level = self.Param("LowerLevel", 0.3)

        self._prev_osc = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DeMarkerPeriod(self):
        return self._demarker_period.Value

    @DeMarkerPeriod.setter
    def DeMarkerPeriod(self, value):
        self._demarker_period.Value = value

    @property
    def UpperLevel(self):
        return self._upper_level.Value

    @UpperLevel.setter
    def UpperLevel(self, value):
        self._upper_level.Value = value

    @property
    def LowerLevel(self):
        return self._lower_level.Value

    @LowerLevel.setter
    def LowerLevel(self, value):
        self._lower_level.Value = value

    def OnReseted(self):
        super(de_marker_gaining_position_volume_strategy, self).OnReseted()
        self._prev_osc = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(de_marker_gaining_position_volume_strategy, self).OnStarted2(time)
        self._prev_osc = 0.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.DeMarkerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        osc_val = float(rsi_value) / 100.0
        upper = float(self.UpperLevel)
        lower = float(self.LowerLevel)

        if self._has_prev:
            # Cross below lower level => oversold => buy
            if self._prev_osc > lower and osc_val <= lower and self.Position <= 0:
                self.BuyMarket()
            # Cross above upper level => overbought => sell
            elif self._prev_osc < upper and osc_val >= upper and self.Position >= 0:
                self.SellMarket()

        self._prev_osc = osc_val
        self._has_prev = True

    def CreateClone(self):
        return de_marker_gaining_position_volume_strategy()
