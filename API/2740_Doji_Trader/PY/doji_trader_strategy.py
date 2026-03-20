import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class doji_trader_strategy(Strategy):
    def __init__(self):
        super(doji_trader_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
        self._start_hour = self.Param("StartHour", 8) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
        self._end_hour = self.Param("EndHour", 17) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
        self._maximum_doji_height = self.Param("MaximumDojiHeight", 1) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Protection")

        self._previous_candle = None
        self._two_ago_candle = None
        self._three_ago_candle = None
        self._pip_size = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_trader_strategy, self).OnReseted()
        self._previous_candle = None
        self._two_ago_candle = None
        self._three_ago_candle = None
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(doji_trader_strategy, self).OnStarted(time)


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
        return doji_trader_strategy()
