import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class prototype_ix_strategy(Strategy):
    """Williams %R + ATR strategy with swing point tracking, breakout detection,
    and ATR-based trailing stop that activates after a configurable delay."""

    def __init__(self):
        super(prototype_ix_strategy, self).__init__()

        self._williams_period = self.Param("WilliamsPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Length of the Williams %R indicator", "Indicators")
        self._criteria_wpr = self.Param("CriteriaWpr", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Criteria WPR", "Absolute threshold for the Williams %R levels", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 40) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Length of the ATR indicator", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier applied to ATR for breakout detection", "Indicators")
        self._zero_bar_delay = self.Param("ZeroBarDelay", 8) \
            .SetDisplay("Zero Bar", "Bars before activating ATR trailing", "Risk Management")
        self._min_target_in_spread = self.Param("MinTargetInSpread", 5.0) \
            .SetDisplay("Min Target Spread", "Minimum target measured in spread multiples", "Risk Management")
        self._tp_sl_criteria = self.Param("TpSlCriteria", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP/SL Criteria", "Required ratio between take-profit and stop-loss", "Risk Management")
        self._max_opened_orders = self.Param("MaxOpenedOrders", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Orders", "Maximum simultaneously opened orders", "General")
        self._max_order_size = self.Param("MaxOrderSize", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Order Size", "Upper bound for calculated order volume", "Risk Management")
        self._risk_delta = self.Param("RiskDelta", 5.0) \
            .SetDisplay("Risk %", "Risk percentage used for position sizing", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used for calculations", "General")

        self._last_swing_high = None
        self._previous_swing_high = None
        self._last_swing_low = None
        self._previous_swing_low = None
        self._tracking_up_swing = False
        self._tracking_down_swing = False
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._initial_stop_price = 0.0
        self._is_long_position = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @property
    def CriteriaWpr(self):
        return self._criteria_wpr.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @property
    def ZeroBarDelay(self):
        return self._zero_bar_delay.Value

    @property
    def MinTargetInSpread(self):
        return self._min_target_in_spread.Value

    @property
    def TpSlCriteria(self):
        return self._tp_sl_criteria.Value

    @property
    def MaxOpenedOrders(self):
        return self._max_opened_orders.Value

    @property
    def MaxOrderSize(self):
        return self._max_order_size.Value

    @property
    def RiskDelta(self):
        return self._risk_delta.Value

    def OnReseted(self):
        super(prototype_ix_strategy, self).OnReseted()
        self._last_swing_high = None
        self._previous_swing_high = None
        self._last_swing_low = None
        self._previous_swing_low = None
        self._tracking_up_swing = False
        self._tracking_down_swing = False
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._initial_stop_price = 0.0
        self._is_long_position = False

    def OnStarted2(self, time):
        super(prototype_ix_strategy, self).OnStarted2(time)

        williams = WilliamsR()
        williams.Length = self.WilliamsPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(williams, atr, self._process_candle).Start()

    def _process_candle(self, candle, wpr_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_swing_points(candle, float(wpr_value))

        atr_val = float(atr_value)
        if atr_val <= 0:
            return

        upper_threshold = -float(self.CriteriaWpr)
        lower_threshold = float(self.CriteriaWpr) - 100.0

        if self.Position == 0:
            self._bars_since_entry = 0

            if float(wpr_value) >= upper_threshold:
                self.BuyMarket()
                self._is_long_position = True
                self._entry_price = float(candle.ClosePrice)
                self._initial_stop_price = float(candle.ClosePrice) - 2.0 * atr_val
            elif float(wpr_value) <= lower_threshold:
                self.SellMarket()
                self._is_long_position = False
                self._entry_price = float(candle.ClosePrice)
                self._initial_stop_price = float(candle.ClosePrice) + 2.0 * atr_val
        else:
            self._bars_since_entry += 1
            self._update_trailing_protection(candle, atr_val)

    def _update_swing_points(self, candle, wpr_value):
        upper_threshold = -float(self.CriteriaWpr)
        lower_threshold = float(self.CriteriaWpr) - 100.0

        if wpr_value >= upper_threshold:
            if not self._tracking_up_swing:
                self._previous_swing_high = self._last_swing_high
                self._last_swing_high = float(candle.HighPrice)
                self._tracking_up_swing = True
                self._tracking_down_swing = False
            elif self._last_swing_high is not None:
                self._last_swing_high = max(self._last_swing_high, float(candle.HighPrice))
        elif wpr_value <= lower_threshold:
            if not self._tracking_down_swing:
                self._previous_swing_low = self._last_swing_low
                self._last_swing_low = float(candle.LowPrice)
                self._tracking_down_swing = True
                self._tracking_up_swing = False
            elif self._last_swing_low is not None:
                self._last_swing_low = min(self._last_swing_low, float(candle.LowPrice))
        else:
            self._tracking_up_swing = False
            self._tracking_down_swing = False

    def _update_trailing_protection(self, candle, atr_value):
        if self.Position == 0:
            return

        reference_price = float(candle.ClosePrice)

        if self._is_long_position and self.Position > 0:
            if reference_price <= self._initial_stop_price:
                self.SellMarket()
                return
            if self._bars_since_entry >= self.ZeroBarDelay:
                atr_stop = reference_price - 2.0 * atr_value
                if atr_stop > self._initial_stop_price:
                    self._initial_stop_price = atr_stop
        elif not self._is_long_position and self.Position < 0:
            if reference_price >= self._initial_stop_price:
                self.BuyMarket()
                return
            if self._bars_since_entry >= self.ZeroBarDelay:
                atr_stop = reference_price + 2.0 * atr_value
                if atr_stop < self._initial_stop_price:
                    self._initial_stop_price = atr_stop

    def CreateClone(self):
        return prototype_ix_strategy()
