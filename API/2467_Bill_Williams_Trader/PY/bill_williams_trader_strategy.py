import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SmoothedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bill_williams_trader_strategy(Strategy):
    def __init__(self):
        super(bill_williams_trader_strategy, self).__init__()

        self._jaw_length = self.Param("JawLength", 13)
        self._teeth_length = self.Param("TeethLength", 8)
        self._lips_length = self.Param("LipsLength", 5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._jaw = None
        self._teeth = None
        self._lips = None
        self._high_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._up_fractal = None
        self._down_fractal = None

    @property
    def JawLength(self):
        return self._jaw_length.Value

    @JawLength.setter
    def JawLength(self, value):
        self._jaw_length.Value = value

    @property
    def TeethLength(self):
        return self._teeth_length.Value

    @TeethLength.setter
    def TeethLength(self, value):
        self._teeth_length.Value = value

    @property
    def LipsLength(self):
        return self._lips_length.Value

    @LipsLength.setter
    def LipsLength(self, value):
        self._lips_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(bill_williams_trader_strategy, self).OnStarted2(time)

        self._high_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._up_fractal = None
        self._down_fractal = None

        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = self.JawLength
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = self.TeethLength
        self._lips = SmoothedMovingAverage()
        self._lips.Length = self.LipsLength

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

        for i in range(4):
            self._high_buffer[i] = self._high_buffer[i + 1]
            self._low_buffer[i] = self._low_buffer[i + 1]
        self._high_buffer[4] = h
        self._low_buffer[4] = l

        if not is_final:
            return
        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed:
            return

        h2 = self._high_buffer[2]
        if h2 > self._high_buffer[0] and h2 > self._high_buffer[1] and h2 > self._high_buffer[3] and h2 > self._high_buffer[4]:
            self._up_fractal = h2

        l2 = self._low_buffer[2]
        if l2 < self._low_buffer[0] and l2 < self._low_buffer[1] and l2 < self._low_buffer[3] and l2 < self._low_buffer[4]:
            self._down_fractal = l2

        jaw_val = float(jaw_result)
        teeth_val = float(teeth_result)
        lips_val = float(lips_result)
        close = float(candle.ClosePrice)

        pos = float(self.Position)
        vol = float(self.Volume)

        if self._up_fractal is not None and close > self._up_fractal and lips_val > teeth_val and teeth_val > jaw_val and pos <= 0:
            self.BuyMarket(vol + abs(pos))
            self._up_fractal = None
        elif self._down_fractal is not None and close < self._down_fractal and lips_val < teeth_val and teeth_val < jaw_val and pos >= 0:
            self.SellMarket(vol + abs(pos))
            self._down_fractal = None

        pos = float(self.Position)
        if pos > 0 and close < lips_val:
            self.SellMarket(pos)
        elif pos < 0 and close > lips_val:
            self.BuyMarket(-pos)

    def OnReseted(self):
        super(bill_williams_trader_strategy, self).OnReseted()
        self._jaw = None
        self._teeth = None
        self._lips = None
        self._high_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._low_buffer = [0.0, 0.0, 0.0, 0.0, 0.0]
        self._up_fractal = None
        self._down_fractal = None

    def CreateClone(self):
        return bill_williams_trader_strategy()
