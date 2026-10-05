import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class atr_stop_loss_double_sma_strategy(Strategy):
    """
    ATR stop-loss double SMA strategy.
    A fast SMA crossing above the slow SMA goes long and a cross below goes short, reversing an opposite position. Each entry
    fixes a stop-loss AtrMultiplier ATRs from the entry close; an AtrMultiplier of 0 disables it.
    """

    def __init__(self):
        super(atr_stop_loss_double_sma_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast SMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow SMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetNotNegative() \
            .SetDisplay("ATR Multiplier", "Stop-loss distance in ATR multiples, 0 disables it", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = 0.0

    def OnReseted(self):
        super(atr_stop_loss_double_sma_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(atr_stop_loss_double_sma_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        multiplier = float(self._atr_multiplier.Value)
        distance = multiplier * float(atr_value)

        if prev_fast <= prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - distance
        elif prev_fast >= prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + distance
        elif multiplier > 0:
            if self.Position > 0 and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(self.Position)
            elif self.Position < 0 and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return atr_stop_loss_double_sma_strategy()
