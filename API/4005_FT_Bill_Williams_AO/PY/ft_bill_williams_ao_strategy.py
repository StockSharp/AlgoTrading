import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ft_bill_williams_ao_strategy(Strategy):
    """
    FT Bill Williams AO: Awesome Oscillator zero-line cross with SMA teeth filter.
    Buys when AO crosses above zero and price above teeth SMA.
    Sells when AO crosses below zero and price below teeth SMA.
    """

    def __init__(self):
        super(ft_bill_williams_ao_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._jaw_period = self.Param("JawPeriod", 13) \
            .SetDisplay("Jaw Period", "Alligator jaw SMA period", "Alligator")
        self._teeth_period = self.Param("TeethPeriod", 8) \
            .SetDisplay("Teeth Period", "Alligator teeth SMA period", "Alligator")
        self._lips_period = self.Param("LipsPeriod", 5) \
            .SetDisplay("Lips Period", "Alligator lips SMA period", "Alligator")

        self._prev_ao = 0.0
        self._prev_teeth = 0.0
        self._is_ready = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ft_bill_williams_ao_strategy, self).OnReseted()
        self._prev_ao = 0.0
        self._prev_teeth = 0.0
        self._is_ready = False

    def OnStarted(self, time):
        super(ft_bill_williams_ao_strategy, self).OnStarted(time)

        ao = AwesomeOscillator()
        teeth = SimpleMovingAverage()
        teeth.Length = self._teeth_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ao, teeth, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ao)
            self.DrawIndicator(area, teeth)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ao_val, teeth_val):
        if candle.State != CandleStates.Finished:
            return

        ao_val = float(ao_val)
        teeth_val = float(teeth_val)

        if not self._is_ready:
            self._prev_ao = ao_val
            self._prev_teeth = teeth_val
            self._is_ready = True
            return

        close = float(candle.ClosePrice)

        if self._prev_ao <= 0 and ao_val > 0 and close > teeth_val:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_ao >= 0 and ao_val < 0 and close < teeth_val:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_ao = ao_val
        self._prev_teeth = teeth_val

    def CreateClone(self):
        return ft_bill_williams_ao_strategy()
