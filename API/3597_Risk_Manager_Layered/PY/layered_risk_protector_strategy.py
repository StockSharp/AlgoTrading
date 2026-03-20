import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class layered_risk_protector_strategy(Strategy):
    def __init__(self):
        super(layered_risk_protector_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._cci_length = self.Param("CciLength", 100)
        self._cci_level = self.Param("CciLevel", 150.0)

        self._prev_cci = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CciLength(self):
        return self._cci_length.Value

    @CciLength.setter
    def CciLength(self, value):
        self._cci_length.Value = value

    @property
    def CciLevel(self):
        return self._cci_level.Value

    @CciLevel.setter
    def CciLevel(self, value):
        self._cci_level.Value = value

    def OnReseted(self):
        super(layered_risk_protector_strategy, self).OnReseted()
        self._prev_cci = None

    def OnStarted(self, time):
        super(layered_risk_protector_strategy, self).OnStarted(time)
        self._prev_cci = None

        cci = CommodityChannelIndex()
        cci.Length = self.CciLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._process_candle).Start()

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)

        if self._prev_cci is None:
            self._prev_cci = cci_val
            return

        cci_level = float(self.CciLevel)

        # CCI crosses below -level -> buy
        buy_cross = self._prev_cci >= -cci_level and cci_val < -cci_level
        # CCI crosses above +level -> sell
        sell_cross = self._prev_cci <= cci_level and cci_val > cci_level

        if buy_cross:
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_cross:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_cci = cci_val

    def CreateClone(self):
        return layered_risk_protector_strategy()
