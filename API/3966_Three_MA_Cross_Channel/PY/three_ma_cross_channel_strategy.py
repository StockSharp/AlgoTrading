import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA, SimpleMovingAverage as SMA, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_ma_cross_channel_strategy(Strategy):
    def __init__(self):
        super(three_ma_cross_channel_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 2) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._medium_length = self.Param("MediumLength", 4) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._slow_length = self.Param("SlowLength", 30) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._channel_length = self.Param("ChannelLength", 15) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._fast_type = self.Param("FastType", MovingAverageTypes.EMA) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._medium_type = self.Param("MediumType", MovingAverageTypes.EMA) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._slow_type = self.Param("SlowType", MovingAverageTypes.EMA) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._take_profit = self.Param("TakeProfit", 0) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._stop_loss = self.Param("StopLoss", 0) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._use_channel_stop = self.Param("UseChannelStop", True) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages")

        self._fast_ma = null!
        self._medium_ma = null!
        self._slow_ma = null!
        self._channel = null!
        self._prev_fast_above_slow = None
        self._prev_medium_above_slow = None
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_ma_cross_channel_strategy, self).OnReseted()
        self._fast_ma = null!
        self._medium_ma = null!
        self._slow_ma = null!
        self._channel = null!
        self._prev_fast_above_slow = None
        self._prev_medium_above_slow = None
        self._entry_price = None

    def OnStarted(self, time):
        super(three_ma_cross_channel_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__channel = DonchianChannels()
        self.__channel.Length = self.channel_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_fastMa, _mediumMa, _slowMa, self.__channel, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return three_ma_cross_channel_strategy()
