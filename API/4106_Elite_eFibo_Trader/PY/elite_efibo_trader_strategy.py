import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex, AverageTrueRange

class elite_efibo_trader_strategy(Strategy):
    def __init__(self):
        super(elite_efibo_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast MA", "Fast SMA period.", "Indicators")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Slow MA", "Slow SMA period.", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI filter period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._add_count = 0

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
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted2(self, time):
        super(elite_efibo_trader_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._add_count = 0

        self._fast = SimpleMovingAverage()
        self._fast.Length = self.FastLength
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.SlowLength
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast, self._slow, self._rsi, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        rv = float(rsi_val)
        av = float(atr_val)

        if self._prev_fast == 0 or self._prev_slow == 0 or av <= 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        close = float(candle.ClosePrice)

        # Position management
        if self.Position > 0:
            # Take profit or stop
            if close >= self._entry_price + av * 3.0:
                self.SellMarket()
                self._entry_price = 0.0
                self._add_count = 0
            elif close <= self._entry_price - av * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
                self._add_count = 0
            elif self._add_count < 2 and close <= self._entry_price - av * 0.8 and rv < 40:
                # Fibonacci add: buy more on pullback
                self._entry_price = (self._entry_price + close) / 2.0
                self._add_count += 1
                self.BuyMarket()
        elif self.Position < 0:
            if close <= self._entry_price - av * 3.0:
                self.BuyMarket()
                self._entry_price = 0.0
                self._add_count = 0
            elif close >= self._entry_price + av * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0
                self._add_count = 0
            elif self._add_count < 2 and close >= self._entry_price + av * 0.8 and rv > 60:
                self._entry_price = (self._entry_price + close) / 2.0
                self._add_count += 1
                self.SellMarket()

        # Entry: MA crossover with RSI confirmation
        if self.Position == 0:
            if self._prev_fast <= self._prev_slow and fv > sv and rv > 50:
                self._entry_price = close
                self._add_count = 0
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and fv < sv and rv < 50:
                self._entry_price = close
                self._add_count = 0
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(elite_efibo_trader_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._add_count = 0

    def CreateClone(self):
        return elite_efibo_trader_strategy()
