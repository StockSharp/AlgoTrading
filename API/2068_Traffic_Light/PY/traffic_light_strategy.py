import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class traffic_light_strategy(Strategy):

    def __init__(self):
        super(traffic_light_strategy, self).__init__()

        self._red_ma_period = self.Param("RedMaPeriod", 50) \
            .SetDisplay("Red MA", "EMA period representing the slow trend", "Parameters")
        self._yellow_ma_period = self.Param("YellowMaPeriod", 25) \
            .SetDisplay("Yellow MA", "EMA period representing the medium trend", "Parameters")
        self._green_ma_period = self.Param("GreenMaPeriod", 5) \
            .SetDisplay("Green MA", "EMA period representing the fast trend", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

    @property
    def RedMaPeriod(self):
        return self._red_ma_period.Value

    @RedMaPeriod.setter
    def RedMaPeriod(self, value):
        self._red_ma_period.Value = value

    @property
    def YellowMaPeriod(self):
        return self._yellow_ma_period.Value

    @YellowMaPeriod.setter
    def YellowMaPeriod(self, value):
        self._yellow_ma_period.Value = value

    @property
    def GreenMaPeriod(self):
        return self._green_ma_period.Value

    @GreenMaPeriod.setter
    def GreenMaPeriod(self, value):
        self._green_ma_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(traffic_light_strategy, self).OnStarted(time)

        red_ma = ExponentialMovingAverage()
        red_ma.Length = self.RedMaPeriod
        yellow_ma = ExponentialMovingAverage()
        yellow_ma.Length = self.YellowMaPeriod
        green_ma = ExponentialMovingAverage()
        green_ma.Length = self.GreenMaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(red_ma, yellow_ma, green_ma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, red, yellow, green):
        if candle.State != CandleStates.Finished:
            return

        red_f = float(red)
        yellow_f = float(yellow)
        green_f = float(green)
        price = float(candle.ClosePrice)

        if green_f > yellow_f and yellow_f > red_f and price > green_f and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif green_f < yellow_f and yellow_f < red_f and price < green_f and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and green_f < yellow_f:
            self.SellMarket()
        elif self.Position < 0 and green_f > yellow_f:
            self.BuyMarket()

    def OnReseted(self):
        super(traffic_light_strategy, self).OnReseted()

    def CreateClone(self):
        return traffic_light_strategy()
