import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_distance_strategy(Strategy):
    def __init__(self):
        super(bollinger_bands_distance_strategy, self).__init__()

        self._bb_period = self.Param("BollingerPeriod", 20)
        self._bb_deviation = self.Param("BollingerDeviation", 2.0)
        self._band_distance = self.Param("BandDistance", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._closes = []

    @property
    def BollingerPeriod(self):
        return self._bb_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bb_period.Value = value

    @property
    def BollingerDeviation(self):
        return self._bb_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bb_deviation.Value = value

    @property
    def BandDistance(self):
        return self._band_distance.Value

    @BandDistance.setter
    def BandDistance(self, value):
        self._band_distance.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(bollinger_bands_distance_strategy, self).OnStarted2(time)

        self._closes = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_price = float(candle.ClosePrice)
        period = int(self.BollingerPeriod)

        self._closes.append(close_price)
        if len(self._closes) > period:
            self._closes.pop(0)

        if len(self._closes) < period:
            return

        total = 0.0
        for c in self._closes:
            total += c
        middle = total / len(self._closes)

        variance = 0.0
        for c in self._closes:
            delta = c - middle
            variance += delta * delta
        std_dev = math.sqrt(variance / len(self._closes))

        dev = float(self.BollingerDeviation)
        upper = middle + dev * std_dev
        lower = middle - dev * std_dev

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        distance = float(self.BandDistance) * step

        if self.Position > 0 and close_price >= middle:
            self.SellMarket()
        elif self.Position < 0 and close_price <= middle:
            self.BuyMarket()

        if self.Position == 0:
            if close_price > upper + distance:
                self.SellMarket()
            elif close_price < lower - distance:
                self.BuyMarket()

    def OnReseted(self):
        super(bollinger_bands_distance_strategy, self).OnReseted()
        self._closes = []

    def CreateClone(self):
        return bollinger_bands_distance_strategy()
