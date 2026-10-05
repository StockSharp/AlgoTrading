import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_average_crossover_strategy(Strategy):
    """
    Moving average crossover strategy.
    Goes long when the short SMA crosses above the long SMA and short when it crosses below,
    reversing the position on the opposite crossover.
    """

    def __init__(self):
        super(moving_average_crossover_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 9).SetGreaterThanZero().SetDisplay("Short Length", "Short SMA length", "Indicators")
        self._long_length = self.Param("LongLength", 21).SetGreaterThanZero().SetDisplay("Long Length", "Long SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None

    def OnReseted(self):
        super(moving_average_crossover_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(moving_average_crossover_strategy, self).OnStarted2(time)

        self._reset_state()

        short_sma = SimpleMovingAverage()
        short_sma.Length = self._short_length.Value
        long_sma = SimpleMovingAverage()
        long_sma.Length = self._long_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_sma, long_sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_sma)
            self.DrawIndicator(area, long_sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not long_value.IsFormed:
            return

        short_ma = short_value.GetValue[Decimal](None)
        long_ma = long_value.GetValue[Decimal](None)

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = short_ma
        self._prev_long = long_ma

        if prev_short is None or prev_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_short <= prev_long and short_ma > long_ma
        cross_down = prev_short >= prev_long and short_ma < long_ma

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return moving_average_crossover_strategy()
