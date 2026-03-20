import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class range_breakout_strategy(Strategy):
    def __init__(self):
        super(range_breakout_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 14) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")

        self._highest = None
        self._lowest = None

    @property
    def channel_period(self):
        return self._channel_period.Value

    def OnReseted(self):
        super(range_breakout_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None

    def OnStarted(self, time):
        super(range_breakout_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._highest, self._lowest, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        close = float(candle.ClosePrice)
        h = float(high_val)
        l = float(low_val)

        if close >= h and self.Position <= 0:
            self.BuyMarket()
        elif close <= l and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return range_breakout_strategy()
