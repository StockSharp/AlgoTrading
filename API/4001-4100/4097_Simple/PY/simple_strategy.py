import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import WeightedMovingAverage, SimpleMovingAverage, AverageTrueRange

class simple_strategy(Strategy):
    def __init__(self):
        super(simple_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_length = self.Param("FastLength", 50) \
            .SetDisplay("Fast WMA", "Fast weighted moving average period.", "Indicators")
        self._slow_length = self.Param("SlowLength", 200) \
            .SetDisplay("Slow SMA", "Slow simple moving average period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for stops.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted2(self, time):
        super(simple_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

        self._fast = WeightedMovingAverage()
        self._fast.Length = self.FastLength
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.SlowLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast, self._slow, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        av = float(atr_val)

        if self._prev_fast == 0 or self._prev_slow == 0 or av <= 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        close = float(candle.ClosePrice)

        # Exit management
        if self.Position > 0:
            if close <= self._entry_price - av * 2.0 or close >= self._entry_price + av * 3.0:
                self.SellMarket()
                self._entry_price = 0.0
            elif fv < sv:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close >= self._entry_price + av * 2.0 or close <= self._entry_price - av * 3.0:
                self.BuyMarket()
                self._entry_price = 0.0
            elif fv > sv:
                self.BuyMarket()
                self._entry_price = 0.0

        # Entry: WMA/SMA crossover
        if self.Position == 0:
            if self._prev_fast <= self._prev_slow and fv > sv:
                self._entry_price = close
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and fv < sv:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(simple_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return simple_strategy()
