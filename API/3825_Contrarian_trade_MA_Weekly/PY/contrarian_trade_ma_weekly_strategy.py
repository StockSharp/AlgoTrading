import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class contrarian_trade_ma_weekly_strategy(Strategy):
    def __init__(self):
        super(contrarian_trade_ma_weekly_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 14) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._channel_period = self.Param("ChannelPeriod", 10) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")

        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(contrarian_trade_ma_weekly_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(contrarian_trade_ma_weekly_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.ma_period
        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return contrarian_trade_ma_weekly_strategy()
