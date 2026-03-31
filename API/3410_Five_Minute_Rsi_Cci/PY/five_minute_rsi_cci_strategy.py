import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class five_minute_rsi_cci_strategy(Strategy):
    def __init__(self):
        super(five_minute_rsi_cci_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._cci_period = self.Param("CciPeriod", 14)
        self._bullish_level = self.Param("BullishLevel", 55.0)
        self._bearish_level = self.Param("BearishLevel", 45.0)

        self._was_bullish = False
        self._has_prev_signal = False

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
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def BullishLevel(self):
        return self._bullish_level.Value

    @BullishLevel.setter
    def BullishLevel(self, value):
        self._bullish_level.Value = value

    @property
    def BearishLevel(self):
        return self._bearish_level.Value

    @BearishLevel.setter
    def BearishLevel(self, value):
        self._bearish_level.Value = value

    def OnReseted(self):
        super(five_minute_rsi_cci_strategy, self).OnReseted()
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted2(self, time):
        super(five_minute_rsi_cci_strategy, self).OnStarted2(time)
        self._has_prev_signal = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, cci, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        is_bullish = float(rsi_value) > self.BullishLevel and float(cci_value) > 0

        if self._has_prev_signal and is_bullish != self._was_bullish:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
            elif not is_bullish and float(rsi_value) < self.BearishLevel and float(cci_value) < 0 and self.Position >= 0:
                self.SellMarket()

        self._was_bullish = is_bullish
        self._has_prev_signal = True

    def CreateClone(self):
        return five_minute_rsi_cci_strategy()
