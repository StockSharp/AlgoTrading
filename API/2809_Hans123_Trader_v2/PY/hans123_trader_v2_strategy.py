import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class hans123_trader_v2_strategy(Strategy):
    def __init__(self):
        super(hans123_trader_v2_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._breakout_period = self.Param("BreakoutPeriod", 10) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Stop Loss (pips)", "Stop distance", "Risk")

        self._highest = None
        self._lowest = None
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._highest_stop_price = 0.0
        self._prev_breakout_high = None
        self._prev_breakout_low = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hans123_trader_v2_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._entry_price = 0.0
        self._pip_size = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._highest_stop_price = 0.0
        self._prev_breakout_high = None
        self._prev_breakout_low = None

    def OnStarted(self, time):
        super(hans123_trader_v2_strategy, self).OnStarted(time)

        self.__highest = Highest()
        self.__highest.Length = self.breakout_period
        self.__lowest = Lowest()
        self.__lowest.Length = self.breakout_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__highest, self.__lowest, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return hans123_trader_v2_strategy()
