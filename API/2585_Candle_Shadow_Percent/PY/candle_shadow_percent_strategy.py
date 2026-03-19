import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class candle_shadow_percent_strategy(Strategy):
    """
    Candle shadow percent strategy.
    Trades when a candle shows an extended wick compared to its body.
    """

    def __init__(self):
        super(candle_shadow_percent_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
        self._risk_percent = self.Param("RiskPercent", 5.0) \
            .SetDisplay("Risk %", "Risk percentage per trade", "Risk")
        self._min_body_pips = self.Param("MinBodyPips", 300) \
            .SetDisplay("Minimum Body", "Minimum candle body size in pips", "Pattern")
        self._enable_top_shadow = self.Param("EnableTopShadow", True) \
            .SetDisplay("Use Top Shadow", "Enable sell signals from upper wicks", "Pattern")
        self._top_shadow_percent = self.Param("TopShadowPercent", 30.0) \
            .SetDisplay("Top Shadow %", "Upper wick percentage threshold", "Pattern")
        self._top_shadow_is_minimum = self.Param("TopShadowIsMinimum", True) \
            .SetDisplay("Top Shadow Uses Min", "If true threshold is treated as minimum", "Pattern")
        self._enable_lower_shadow = self.Param("EnableLowerShadow", True) \
            .SetDisplay("Use Lower Shadow", "Enable buy signals from lower wicks", "Pattern")
        self._lower_shadow_percent = self.Param("LowerShadowPercent", 80.0) \
            .SetDisplay("Lower Shadow %", "Lower wick percentage threshold", "Pattern")
        self._lower_shadow_is_minimum = self.Param("LowerShadowIsMinimum", True) \
            .SetDisplay("Lower Shadow Uses Min", "If true threshold is treated as minimum", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for pattern detection", "Data")

        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(candle_shadow_percent_strategy, self).OnReseted()
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = None

    def OnStarted(self, time):
        super(candle_shadow_percent_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_pip_size(self):
        return self.Security.PriceStep if self.Security.PriceStep is not None else 1.0

    def _check_threshold(self, ratio, threshold, is_minimum):
        return ratio >= threshold if is_minimum else ratio <= threshold

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Manage open position
        self._manage_open_position(candle)

        pip_size = self._get_pip_size()
        min_body = self._min_body_pips.Value * pip_size

        body = abs(candle.ClosePrice - candle.OpenPrice)
        if body < min_body or body <= 0:
            return

        upper_shadow = candle.HighPrice - max(candle.OpenPrice, candle.ClosePrice)
        lower_shadow = min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice

        top_ratio = upper_shadow / body * 100.0 if body > 0 else 0.0
        lower_ratio = lower_shadow / body * 100.0 if body > 0 else 0.0

        top_signal = (self._enable_top_shadow.Value and upper_shadow > 0
                      and self._check_threshold(top_ratio, self._top_shadow_percent.Value, self._top_shadow_is_minimum.Value))
        lower_signal = (self._enable_lower_shadow.Value and lower_shadow > 0
                        and self._check_threshold(lower_ratio, self._lower_shadow_percent.Value, self._lower_shadow_is_minimum.Value))

        if top_signal and lower_signal:
            if top_ratio > lower_ratio:
                lower_signal = False
            else:
                top_signal = False

        if top_signal and self.Position <= 0:
            self._enter_short(candle, pip_size)
        elif lower_signal and self.Position >= 0:
            self._enter_long(candle, pip_size)

    def _manage_open_position(self, candle):
        if self.Position > 0:
            stop_hit = self._long_stop is not None and candle.LowPrice <= self._long_stop
            take_hit = self._long_take is not None and candle.HighPrice >= self._long_take
            if stop_hit or take_hit:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
                self._entry_price = None
        elif self.Position < 0:
            stop_hit = self._short_stop is not None and candle.HighPrice >= self._short_stop
            take_hit = self._short_take is not None and candle.LowPrice <= self._short_take
            if stop_hit or take_hit:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
                self._entry_price = None

    def _enter_long(self, candle, pip_size):
        stop_distance = self._stop_loss_pips.Value * pip_size
        if stop_distance <= 0:
            return
        take_distance = self._take_profit_pips.Value * pip_size
        entry_price = float(candle.ClosePrice)
        stop_price = entry_price - stop_distance
        take_price = entry_price + take_distance if take_distance > 0 else None

        self.BuyMarket()
        self._long_stop = stop_price
        self._long_take = take_price
        self._short_stop = None
        self._short_take = None
        self._entry_price = entry_price

    def _enter_short(self, candle, pip_size):
        stop_distance = self._stop_loss_pips.Value * pip_size
        if stop_distance <= 0:
            return
        take_distance = self._take_profit_pips.Value * pip_size
        entry_price = float(candle.ClosePrice)
        stop_price = entry_price + stop_distance
        take_price = entry_price - take_distance if take_distance > 0 else None

        self.SellMarket()
        self._short_stop = stop_price
        self._short_take = take_price
        self._long_stop = None
        self._long_take = None
        self._entry_price = entry_price

    def CreateClone(self):
        return candle_shadow_percent_strategy()
