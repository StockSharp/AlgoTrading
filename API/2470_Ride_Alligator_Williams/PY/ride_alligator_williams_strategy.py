import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ride_alligator_williams_strategy(Strategy):
    def __init__(self):
        super(ride_alligator_williams_strategy, self).__init__()

        self._base_period = self.Param("BasePeriod", 8)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._jaw = None
        self._teeth = None
        self._lips = None
        self._prev_lips_above_jaw = False
        self._stop_price = None

    @property
    def BasePeriod(self):
        return self._base_period.Value

    @BasePeriod.setter
    def BasePeriod(self, value):
        self._base_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(ride_alligator_williams_strategy, self).OnStarted2(time)

        self._prev_lips_above_jaw = False
        self._stop_price = None

        bp = int(self.BasePeriod)
        phi = 1.61803398874989
        a1 = int(round(bp * phi))
        a2 = int(round(a1 * phi))
        a3 = int(round(a2 * phi))

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = a3
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = a2
        self._lips = SmoothedMovingAverage()
        self._lips.Length = a1

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        median = (h + l) / 2.0
        is_final = candle.State == CandleStates.Finished

        jaw_input = DecimalIndicatorValue(self._jaw, Decimal(median), candle.ServerTime)
        jaw_input.IsFinal = is_final
        jaw_result = self._jaw.Process(jaw_input)

        teeth_input = DecimalIndicatorValue(self._teeth, Decimal(median), candle.ServerTime)
        teeth_input.IsFinal = is_final
        teeth_result = self._teeth.Process(teeth_input)

        lips_input = DecimalIndicatorValue(self._lips, Decimal(median), candle.ServerTime)
        lips_input.IsFinal = is_final
        lips_result = self._lips.Process(lips_input)

        if not is_final:
            return

        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return

        jaw_val = float(jaw_result)
        teeth_val = float(teeth_result)
        lips_val = float(lips_result)
        close = float(candle.ClosePrice)

        lips_above_jaw = lips_val > jaw_val
        lips_below_jaw = lips_val < jaw_val
        teeth_above_jaw = teeth_val > jaw_val
        teeth_below_jaw = teeth_val < jaw_val

        pos = float(self.Position)

        if pos <= 0 and not self._prev_lips_above_jaw and lips_above_jaw and teeth_below_jaw:
            self.BuyMarket()
            self._stop_price = None
        elif pos >= 0 and self._prev_lips_above_jaw and lips_below_jaw and teeth_above_jaw:
            self.SellMarket()
            self._stop_price = None

        pos = float(self.Position)
        if pos > 0:
            if jaw_val < close:
                step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
                if self._stop_price is None or jaw_val > self._stop_price + step:
                    self._stop_price = jaw_val
            if self._stop_price is not None and close <= self._stop_price:
                self.SellMarket(pos)
                self._stop_price = None
        elif pos < 0:
            if jaw_val > close:
                step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
                if self._stop_price is None or jaw_val < self._stop_price - step:
                    self._stop_price = jaw_val
            if self._stop_price is not None and close >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._stop_price = None

        self._prev_lips_above_jaw = lips_above_jaw

    def OnReseted(self):
        super(ride_alligator_williams_strategy, self).OnReseted()
        self._jaw = None
        self._teeth = None
        self._lips = None
        self._prev_lips_above_jaw = False
        self._stop_price = None

    def CreateClone(self):
        return ride_alligator_williams_strategy()
