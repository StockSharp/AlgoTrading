import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class prototype_ix_strategy(Strategy):
    def __init__(self):
        super(prototype_ix_strategy, self).__init__()

        self._williams_period = self.Param("WilliamsPeriod", 8) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._criteria_wpr = self.Param("CriteriaWpr", 25) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 40) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._zero_bar_delay = self.Param("ZeroBarDelay", 8) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._min_target_in_spread = self.Param("MinTargetInSpread", 5) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._tp_sl_criteria = self.Param("TpSlCriteria", 2.0) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._max_opened_orders = self.Param("MaxOpenedOrders", 1) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._max_order_size = self.Param("MaxOrderSize", 5) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._risk_delta = self.Param("RiskDelta", 5.0) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")

        self._last_swing_high = None
        self._previous_swing_high = None
        self._last_swing_low = None
        self._previous_swing_low = None
        self._tracking_up_swing = False
        self._tracking_down_swing = False
        self._bars_since_entry = 0.0
        self._entry_price = 0.0
        self._initial_stop_price = 0.0
        self._is_long_position = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(prototype_ix_strategy, self).OnReseted()
        self._last_swing_high = None
        self._previous_swing_high = None
        self._last_swing_low = None
        self._previous_swing_low = None
        self._tracking_up_swing = False
        self._tracking_down_swing = False
        self._bars_since_entry = 0.0
        self._entry_price = 0.0
        self._initial_stop_price = 0.0
        self._is_long_position = False

    def OnStarted(self, time):
        super(prototype_ix_strategy, self).OnStarted(time)

        self._williams = WilliamsR()
        self._williams.Length = self.williams_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._williams, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return prototype_ix_strategy()
