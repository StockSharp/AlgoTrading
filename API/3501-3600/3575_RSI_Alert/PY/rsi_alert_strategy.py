import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_alert_strategy(Strategy):
    def __init__(self):
        super(rsi_alert_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._overbought_level = self.Param("OverboughtLevel", 70.0)
        self._oversold_level = self.Param("OversoldLevel", 30.0)

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
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overbought_level.Value = value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversold_level.Value = value

    def OnReseted(self):
        super(rsi_alert_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(rsi_alert_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        buy_signal = rsi_val <= float(self.OversoldLevel)
        sell_signal = rsi_val >= float(self.OverboughtLevel)

        if buy_signal and self.Position == 0:
            self.BuyMarket()
        elif sell_signal and self.Position == 0:
            self.SellMarket()

    def CreateClone(self):
        return rsi_alert_strategy()
