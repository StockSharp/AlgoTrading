import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class grail_expert_ma_strategy(Strategy):
    """
    Grail Expert MA: SMA crossover with ATR-based stop loss.
    Enters on fast/slow SMA crossover, exits on reverse crossover
    or if price moves 2x ATR against position.
    """

    def __init__(self):
        super(grail_expert_ma_strategy, self).__init__()
        self._fast_sma_length = self.Param("FastSmaLength", 10) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 40) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grail_expert_ma_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(grail_expert_ma_strategy, self).OnStarted2(time)

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self._fast_sma_length.Value
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self._slow_sma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)
        close = float(candle.ClosePrice)

        if self._prev_fast == 0.0 or self._prev_slow == 0.0 or atr <= 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        if self.Position > 0:
            if fast < slow and self._prev_fast >= self._prev_slow:
                self.SellMarket()
                self._entry_price = 0.0
            elif close <= self._entry_price - atr * 2.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if fast > slow and self._prev_fast <= self._prev_slow:
                self.BuyMarket()
                self._entry_price = 0.0
            elif close >= self._entry_price + atr * 2.0:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if fast > slow and self._prev_fast <= self._prev_slow:
                self._entry_price = close
                self.BuyMarket()
            elif fast < slow and self._prev_fast >= self._prev_slow:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return grail_expert_ma_strategy()
