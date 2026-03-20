import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class indices_tester_strategy(Strategy):
    def __init__(self):
        super(indices_tester_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._session_start = self.Param("SessionStart", new TimeSpan(0, 0, 0) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._session_end = self.Param("SessionEnd", new TimeSpan(23, 0, 0) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._close_time = self.Param("CloseTime", new TimeSpan(23, 30, 0) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._daily_trade_limit = self.Param("DailyTradeLimit", 1) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._max_open_positions = self.Param("MaxOpenPositions", 1) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General")

        self._current_day = None
        self._trades_opened_today = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(indices_tester_strategy, self).OnReseted()
        self._current_day = None
        self._trades_opened_today = 0.0

    def OnStarted(self, time):
        super(indices_tester_strategy, self).OnStarted(time)


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
        return indices_tester_strategy()
