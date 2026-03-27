import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_n_positions_v2_strategy(Strategy):
    def __init__(self):
        super(bollinger_bands_n_positions_v2_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period used for Bollinger Bands.", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands.", "Indicators")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Max Positions", "Maximum number of stacked entries per direction.", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 30.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips.", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 60.0) \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips.", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop offset in pips.", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 1.0) \
            .SetDisplay("Trailing Step (pips)", "Extra profit in pips before trailing stop is adjusted.", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for Bollinger analysis.", "General")
        self._pip_value = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_entry_count = 0
        self._short_entry_count = 0
        self._long_stop_price = None
        self._long_take_profit_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None
        self._bollinger = None

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def max_positions(self):
        return self._max_positions.Value
    @property
    def stop_loss_pips(self):
        return self._stop_loss_pips.Value
    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value
    @property
    def trailing_stop_pips(self):
        return self._trailing_stop_pips.Value
    @property
    def trailing_step_pips(self):
        return self._trailing_step_pips.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_bands_n_positions_v2_strategy, self).OnReseted()
        self._pip_value = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_stop_distance = 0.0
        self._trailing_step_distance = 0.0
        self._reset_long_state()
        self._reset_short_state()

    def OnStarted(self, time):
        super(bollinger_bands_n_positions_v2_strategy, self).OnStarted(time)
        self._pip_value = self._calculate_pip_value()
        self._update_risk_distances()
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_period
        self._bollinger.Width = self.bollinger_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        civ = CandleIndicatorValue(self._bollinger, candle)
        civ.IsFinal = True
        indicator_value = self._bollinger.Process(civ)
        if indicator_value.IsEmpty or not self._bollinger.IsFormed:
            return
        self._update_risk_distances()
        upper = indicator_value.UpBand
        lower = indicator_value.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        self._handle_risk_management(candle)
        if float(candle.ClosePrice) > upper:
            self._try_enter_long(candle)
            return
        if float(candle.ClosePrice) < lower:
            self._try_enter_short(candle)

    def _handle_risk_management(self, candle):
        if self._long_entry_count > 0 and self.Position > 0:
            if self._long_take_profit_price is not None and float(candle.HighPrice) >= self._long_take_profit_price:
                self.SellMarket()
                self._reset_long_state()
                return
            if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
                self.SellMarket()
                self._reset_long_state()
                return
            self._update_long_trailing(candle)
        elif self.Position <= 0:
            self._reset_long_state()

        if self._short_entry_count > 0 and self.Position < 0:
            if self._short_take_profit_price is not None and float(candle.LowPrice) <= self._short_take_profit_price:
                self.BuyMarket()
                self._reset_short_state()
                return
            if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
                self.BuyMarket()
                self._reset_short_state()
                return
            self._update_short_trailing(candle)
        elif self.Position >= 0:
            self._reset_short_state()

    def _try_enter_long(self, candle):
        if self._long_entry_count >= self.max_positions:
            return
        if self.Position < 0:
            self.BuyMarket()
            self._reset_short_state()
        self.BuyMarket()
        entry_price = float(candle.ClosePrice)
        self._long_entry_count += 1
        self._long_entry_price = entry_price
        if self.stop_loss_pips > 0:
            self._long_stop_price = self._long_entry_price - self._stop_loss_distance
        if self.take_profit_pips > 0:
            self._long_take_profit_price = self._long_entry_price + self._take_profit_distance

    def _try_enter_short(self, candle):
        if self._short_entry_count >= self.max_positions:
            return
        if self.Position > 0:
            self.SellMarket()
            self._reset_long_state()
        self.SellMarket()
        entry_price = float(candle.ClosePrice)
        self._short_entry_count += 1
        self._short_entry_price = entry_price
        if self.stop_loss_pips > 0:
            self._short_stop_price = self._short_entry_price + self._stop_loss_distance
        if self.take_profit_pips > 0:
            self._short_take_profit_price = self._short_entry_price - self._take_profit_distance

    def _update_long_trailing(self, candle):
        if self._trailing_stop_distance <= 0:
            return
        move_from_entry = float(candle.ClosePrice) - self._long_entry_price
        if move_from_entry <= self._trailing_stop_distance + self._trailing_step_distance:
            return
        new_stop = float(candle.ClosePrice) - self._trailing_stop_distance
        if self._long_stop_price is None or new_stop > self._long_stop_price + self._trailing_step_distance:
            self._long_stop_price = new_stop

    def _update_short_trailing(self, candle):
        if self._trailing_stop_distance <= 0:
            return
        move_from_entry = self._short_entry_price - float(candle.ClosePrice)
        if move_from_entry <= self._trailing_stop_distance + self._trailing_step_distance:
            return
        new_stop = float(candle.ClosePrice) + self._trailing_stop_distance
        if self._short_stop_price is None or new_stop < self._short_stop_price - self._trailing_step_distance:
            self._short_stop_price = new_stop

    def _reset_long_state(self):
        self._long_entry_price = 0.0
        self._long_entry_count = 0
        self._long_stop_price = None
        self._long_take_profit_price = None

    def _reset_short_state(self):
        self._short_entry_price = 0.0
        self._short_entry_count = 0
        self._short_stop_price = None
        self._short_take_profit_price = None

    def _update_risk_distances(self):
        self._stop_loss_distance = self.stop_loss_pips * self._pip_value if self.stop_loss_pips > 0 else 0.0
        self._take_profit_distance = self.take_profit_pips * self._pip_value if self.take_profit_pips > 0 else 0.0
        self._trailing_stop_distance = self.trailing_stop_pips * self._pip_value if self.trailing_stop_pips > 0 else 0.0
        self._trailing_step_distance = self.trailing_step_pips * self._pip_value if self.trailing_step_pips > 0 else 0.0

    def _calculate_pip_value(self):
        security = self.Security
        if security is None:
            return 1.0
        step = security.PriceStep
        if step is None or float(step) <= 0:
            return 1.0
        step_val = float(step)
        decimals = self._count_decimals(step_val)
        if decimals == 3 or decimals == 5:
            return step_val * 10.0
        return step_val

    @staticmethod
    def _count_decimals(value):
        value = abs(value)
        decimals = 0
        while value != int(value) and decimals < 10:
            value *= 10
            decimals += 1
        return decimals

    def CreateClone(self):
        return bollinger_bands_n_positions_v2_strategy()
