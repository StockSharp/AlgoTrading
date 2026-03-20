import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class maximus_vx_lite_strategy(Strategy):
    def __init__(self):
        super(maximus_vx_lite_strategy, self).__init__()

        self._delay_open = self.Param("DelayOpen", 2) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._distance_points = self.Param("DistancePoints", 850) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._range_points = self.Param("RangePoints", 500) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._history_depth = self.Param("HistoryDepth", 200) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._range_lookback = self.Param("RangeLookback", 40) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._risk_percent = self.Param("RiskPercent", 5) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")
        self._min_profit_percent = self.Param("MinProfitPercent", 1) \
            .SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules")

        self._history = new()
        self._upper_max = 0.0
        self._upper_min = 0.0
        self._lower_max = 0.0
        self._lower_min = 0.0
        self._price_step = 1
        self._ext_distance = 0.0
        self._ext_range = 0.0
        self._ext_stop_loss = 0.0
        self._last_buy_time = None
        self._last_sell_time = None
        self._last_known_balance = 0.0
        self._active_stop = None
        self._active_take = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(maximus_vx_lite_strategy, self).OnReseted()
        self._history = new()
        self._upper_max = 0.0
        self._upper_min = 0.0
        self._lower_max = 0.0
        self._lower_min = 0.0
        self._price_step = 1
        self._ext_distance = 0.0
        self._ext_range = 0.0
        self._ext_stop_loss = 0.0
        self._last_buy_time = None
        self._last_sell_time = None
        self._last_known_balance = 0.0
        self._active_stop = None
        self._active_take = None

    def OnStarted(self, time):
        super(maximus_vx_lite_strategy, self).OnStarted(time)


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
        return maximus_vx_lite_strategy()
