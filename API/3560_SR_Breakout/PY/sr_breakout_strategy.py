import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class sr_breakout_strategy(Strategy):
    def __init__(self):
        super(sr_breakout_strategy, self).__init__()

        self._lookback_length = self.Param("LookbackLength", 20) \
            .SetDisplay("Lookback", "Number of candles for Donchian channel", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Lookback", "Number of candles for Donchian channel", "Indicators")

        self._high_history = new()
        self._low_history = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sr_breakout_strategy, self).OnReseted()
        self._high_history = new()
        self._low_history = new()

    def OnStarted(self, time):
        super(sr_breakout_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return sr_breakout_strategy()
