import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class volume_trader_v2_strategy(Strategy):
    def __init__(self):
        super(volume_trader_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1) \
            .SetDisplay("Candle Type", "Time frame used to request candles", "Data")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Candle Type", "Time frame used to request candles", "Data")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("Candle Type", "Time frame used to request candles", "Data")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Candle Type", "Time frame used to request candles", "Data")

        self._previous_volume = None
        self._two_bars_ago_volume = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_trader_v2_strategy, self).OnReseted()
        self._previous_volume = None
        self._two_bars_ago_volume = None

    def OnStarted(self, time):
        super(volume_trader_v2_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


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
        return volume_trader_v2_strategy()
