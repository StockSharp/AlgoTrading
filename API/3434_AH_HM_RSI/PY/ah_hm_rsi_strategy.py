import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ah_hm_rsi_strategy(Strategy):
    def __init__(self):
        super(ah_hm_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_low = self.Param("RsiLow", 40.0)
        self._rsi_high = self.Param("RsiHigh", 60.0)

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
    def RsiLow(self):
        return self._rsi_low.Value

    @RsiLow.setter
    def RsiLow(self, value):
        self._rsi_low.Value = value

    @property
    def RsiHigh(self):
        return self._rsi_high.Value

    @RsiHigh.setter
    def RsiHigh(self, value):
        self._rsi_high.Value = value

    def OnStarted2(self, time):
        super(ah_hm_rsi_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        if rng <= 0 or body <= 0:
            return

        upper_shadow = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))
        lower_shadow = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)

        is_hammer = lower_shadow > body * 2 and upper_shadow < body
        is_hanging_man = upper_shadow > body * 2 and lower_shadow < body

        if is_hammer and rsi_val < self.RsiLow and self.Position <= 0:
            self.BuyMarket()
        elif is_hanging_man and rsi_val > self.RsiHigh and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ah_hm_rsi_strategy()
