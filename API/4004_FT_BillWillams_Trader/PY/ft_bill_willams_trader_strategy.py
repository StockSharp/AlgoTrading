import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class ft_bill_willams_trader_strategy(Strategy):
    def __init__(self):
        super(ft_bill_willams_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")
        self._jaw_period = self.Param("JawPeriod", 13) \
            .SetDisplay("Jaw Period", "Alligator jaw SMA period", "Alligator")
        self._teeth_period = self.Param("TeethPeriod", 8) \
            .SetDisplay("Teeth Period", "Alligator teeth SMA period", "Alligator")
        self._lips_period = self.Param("LipsPeriod", 5) \
            .SetDisplay("Lips Period", "Alligator lips SMA period", "Alligator")
        self._fractal_len = self.Param("FractalLen", 5) \
            .SetDisplay("Fractal Length", "Number of bars for fractal detection", "Signals")

        self._high_buf = []
        self._low_buf = []
        self._buf_count = 0
        self._pending_buy_level = None
        self._pending_sell_level = None
        self._prev_jaw = 0.0
        self._prev_teeth = 0.0
        self._prev_lips = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def JawPeriod(self):
        return self._jaw_period.Value

    @property
    def TeethPeriod(self):
        return self._teeth_period.Value

    @property
    def LipsPeriod(self):
        return self._lips_period.Value

    @property
    def FractalLen(self):
        return self._fractal_len.Value

    def OnStarted2(self, time):
        super(ft_bill_willams_trader_strategy, self).OnStarted2(time)

        self._jaw = SimpleMovingAverage()
        self._jaw.Length = self.JawPeriod
        self._teeth = SimpleMovingAverage()
        self._teeth.Length = self.TeethPeriod
        self._lips = SimpleMovingAverage()
        self._lips.Length = self.LipsPeriod

        fractal_len = self.FractalLen
        self._high_buf = [0.0] * fractal_len
        self._low_buf = [0.0] * fractal_len
        self._buf_count = 0
        self._pending_buy_level = None
        self._pending_sell_level = None
        self._prev_jaw = 0.0
        self._prev_teeth = 0.0
        self._prev_lips = 0.0
        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._jaw, self._teeth, self._lips, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, jaw_val, teeth_val, lips_val):
        if candle.State != CandleStates.Finished:
            return

        jaw_val = float(jaw_val)
        teeth_val = float(teeth_val)
        lips_val = float(lips_val)

        self._update_fractals(candle)

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._pending_sell_level is not None and low < self._pending_sell_level:
                self.SellMarket()
                self._entry_price = 0.0

        elif self.Position < 0:
            if self._pending_buy_level is not None and high > self._pending_buy_level:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position <= 0 and self._pending_buy_level is not None:
            if high > self._pending_buy_level and close > teeth_val:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._entry_price = close

        if self.Position >= 0 and self._pending_sell_level is not None:
            if low < self._pending_sell_level and close < teeth_val:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._entry_price = close

        self._prev_jaw = jaw_val
        self._prev_teeth = teeth_val
        self._prev_lips = lips_val

    def _update_fractals(self, candle):
        flen = self.FractalLen
        if flen < 3:
            return

        self._high_buf.pop(0)
        self._high_buf.append(float(candle.HighPrice))
        self._low_buf.pop(0)
        self._low_buf.append(float(candle.LowPrice))

        self._buf_count += 1
        if self._buf_count < flen:
            return

        wing = (flen - 1) // 2
        center = flen - 1 - wing

        center_high = self._high_buf[center]
        is_up = True
        for i in range(flen):
            if i != center and self._high_buf[i] >= center_high:
                is_up = False
                break
        if is_up:
            self._pending_buy_level = center_high

        center_low = self._low_buf[center]
        is_down = True
        for i in range(flen):
            if i != center and self._low_buf[i] <= center_low:
                is_down = False
                break
        if is_down:
            self._pending_sell_level = center_low

    def OnReseted(self):
        super(ft_bill_willams_trader_strategy, self).OnReseted()
        self._high_buf = []
        self._low_buf = []
        self._buf_count = 0
        self._pending_buy_level = None
        self._pending_sell_level = None
        self._prev_jaw = 0.0
        self._prev_teeth = 0.0
        self._prev_lips = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return ft_bill_willams_trader_strategy()
