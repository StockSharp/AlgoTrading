import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class grease_trap_strategy(Strategy):
    """
    Grease trap strategy.
    The Length1 SMA crossing above the Length2 SMA goes long and crossing below goes short, reversing an opposite position. A long closes
    when price reaches the entry plus LongProfit (a fraction of the entry price) and a short when it reaches the entry minus ShortProfit.
    """

    def __init__(self):
        super(grease_trap_strategy, self).__init__()
        self._length1 = self.Param("Length1", 9).SetGreaterThanZero().SetDisplay("Length 1", "Fast SMA length", "Indicators")
        self._length2 = self.Param("Length2", 14).SetGreaterThanZero().SetDisplay("Length 2", "Slow SMA length", "Indicators")
        self._long_profit = self.Param("LongProfit", 0.02).SetNotNegative().SetDisplay("Long Profit", "Long profit target as a fraction of the entry price", "Risk")
        self._short_profit = self.Param("ShortProfit", 0.02).SetNotNegative().SetDisplay("Short Profit", "Short profit target as a fraction of the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(grease_trap_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(grease_trap_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._length1.Value
        slow = SimpleMovingAverage()
        slow.Length = self._length2.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev_fast is None or prev_slow is None:
            return

        close = candle.ClosePrice
        long_profit = Decimal(self._long_profit.Value)
        short_profit = Decimal(self._short_profit.Value)
        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._take_price = close * (Decimal(1) + long_profit) if long_profit > 0 else Decimal(0)
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._take_price = close * (Decimal(1) - short_profit) if short_profit > 0 else Decimal(0)
        elif self.Position > 0 and self._take_price > 0 and candle.HighPrice >= self._take_price:
            self.SellMarket(self.Position)
        elif self.Position < 0 and self._take_price > 0 and candle.LowPrice <= self._take_price:
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return grease_trap_strategy()
