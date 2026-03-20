import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class e_news_lucky_strategy(Strategy):
    def __init__(self):
        super(e_news_lucky_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._distance_pips = self.Param("DistancePips", 20) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._placement_hour = self.Param("PlacementHour", 2) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._cancel_hour = self.Param("CancelHour", 22) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Stop Loss", "Stop loss in pips", "Trading")

        self._pip_size = 0.0
        self._buy_level = None
        self._sell_level = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._pending_active = False
        self._last_was_placement_day = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(e_news_lucky_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._buy_level = None
        self._sell_level = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._pending_active = False
        self._last_was_placement_day = False

    def OnStarted(self, time):
        super(e_news_lucky_strategy, self).OnStarted(time)


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
        return e_news_lucky_strategy()
