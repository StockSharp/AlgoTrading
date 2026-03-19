import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class dealers_trade_v751_rivot_strategy(Strategy):
    """
    Dealers Trade v7.51 strategy ported from MetaTrader 4.
    Builds directional bias from classic pivot and floating pivot levels,
    scales into the bias when price retraces by a fixed pip distance.
    Applies martingale-style position sizing with SL/TP and trailing stop.
    """

    def __init__(self):
        super(dealers_trade_v751_rivot_strategy, self).__init__()
        self._max_trades = self.Param("MaxTrades", 2) \
            .SetDisplay("Max Trades", "Maximum number of martingale entries", "Position Sizing")
        self._pip_distance = self.Param("PipDistance", 10.0) \
            .SetDisplay("Pip Distance", "Distance between averaged entries in pips", "Position Sizing")
        self._take_profit = self.Param("TakeProfit", 15.0) \
            .SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 90.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management")
        self._trailing_stop = self.Param("TrailingStop", 15.0) \
            .SetDisplay("Trailing Stop", "Trailing-stop distance in pips", "Risk Management")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5) \
            .SetDisplay("Volume Multiplier", "Multiplier applied after each new entry", "Position Sizing")
        self._max_volume = self.Param("MaxVolume", 5.0) \
            .SetDisplay("Max Volume", "Upper limit for single-entry volume", "Position Sizing")
        self._gap_threshold = self.Param("GapThreshold", 15.0) \
            .SetDisplay("Gap Threshold", "Minimal pivot gap required to enable trading", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used for pivot calculations", "Signal")

        self._previous_candle = None
        self._pivot_level = 0.0
        self._floating_pivot = 0.0
        self._gap_in_pips = 0.0
        self._last_entry_price = 0.0
        self._average_entry_price = 0.0
        self._trailing_stop_level = None
        self._direction = 0
        self._entries_in_series = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dealers_trade_v751_rivot_strategy, self).OnReseted()
        self._reset_series()
        self._previous_candle = None
        self._pivot_level = 0.0
        self._floating_pivot = 0.0
        self._gap_in_pips = 0.0

    def OnStarted(self, time):
        super(dealers_trade_v751_rivot_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnPositionReceived(self, position):
        super(dealers_trade_v751_rivot_strategy, self).OnPositionReceived(position)
        if self.Position == 0:
            self._reset_series()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._previous_candle is None:
            self._previous_candle = candle
            return

        self._update_pivots(candle)

        if self.Position == 0 and self._entries_in_series > 0:
            self._reset_series()

        if self._entries_in_series > 0:
            self._manage_risk(float(candle.ClosePrice))

        if self._entries_in_series >= self._max_trades.Value:
            self._previous_candle = candle
            return

        if self._direction == 0:
            self._evaluate_direction(candle)

        self._try_enter(candle)
        self._previous_candle = candle

    def _update_pivots(self, candle):
        step = self._get_price_step()
        self._pivot_level = (float(self._previous_candle.HighPrice) + float(self._previous_candle.LowPrice) +
                             float(self._previous_candle.ClosePrice) + float(candle.OpenPrice)) / 4.0
        self._floating_pivot = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        self._gap_in_pips = abs(self._pivot_level - self._floating_pivot) / step if step != 0 else 0.0

    def _evaluate_direction(self, candle):
        price = float(candle.ClosePrice)
        if price > self._pivot_level and price > self._floating_pivot and self._gap_in_pips >= self._gap_threshold.Value:
            self._direction = 1
        elif price < self._pivot_level and price < self._floating_pivot and self._gap_in_pips >= self._gap_threshold.Value:
            self._direction = -1

    def _try_enter(self, candle):
        if self._direction == 0:
            return

        price = float(candle.ClosePrice)
        step = self._get_price_step()
        distance = self._pip_distance.Value * step

        if self._direction > 0:
            if self._entries_in_series == 0 or (self._last_entry_price - price) >= distance:
                self._enter_long(price)
        else:
            if self._entries_in_series == 0 or (price - self._last_entry_price) >= distance:
                self._enter_short(price)

    def _enter_long(self, price):
        self._last_entry_price = price
        existing_volume = abs(float(self.Position))
        if existing_volume <= 0:
            self._average_entry_price = price
        else:
            total = existing_volume + 1.0
            self._average_entry_price = abs((self._average_entry_price * existing_volume + price) / total)
        self._entries_in_series += 1
        self.BuyMarket()

    def _enter_short(self, price):
        self._last_entry_price = price
        existing_volume = abs(float(self.Position))
        if existing_volume <= 0:
            self._average_entry_price = price
        else:
            total = existing_volume + 1.0
            self._average_entry_price = abs((self._average_entry_price * existing_volume * -1.0 + price) / total)
        self._entries_in_series += 1
        self.SellMarket()

    def _manage_risk(self, price):
        if self._entries_in_series == 0:
            self._trailing_stop_level = None
            return

        step = self._get_price_step()
        stop_distance = self._stop_loss.Value * step
        take_distance = self._take_profit.Value * step
        trailing_distance = self._trailing_stop.Value * step

        if self._direction > 0:
            loss_level = self._average_entry_price - stop_distance
            profit_level = self._average_entry_price + take_distance

            if price <= loss_level:
                self.SellMarket()
                self._reset_series()
                return
            if price >= profit_level:
                self.SellMarket()
                self._reset_series()
                return

            if self._trailing_stop.Value > 0:
                candidate = price - trailing_distance
                if self._trailing_stop_level is None or candidate > self._trailing_stop_level:
                    self._trailing_stop_level = candidate
                if self._trailing_stop_level is not None and price <= self._trailing_stop_level:
                    self.SellMarket()
                    self._reset_series()

        elif self._direction < 0:
            loss_level = self._average_entry_price + stop_distance
            profit_level = self._average_entry_price - take_distance

            if price >= loss_level:
                self.BuyMarket()
                self._reset_series()
                return
            if price <= profit_level:
                self.BuyMarket()
                self._reset_series()
                return

            if self._trailing_stop.Value > 0:
                candidate = price + trailing_distance
                if self._trailing_stop_level is None or candidate < self._trailing_stop_level:
                    self._trailing_stop_level = candidate
                if self._trailing_stop_level is not None and price >= self._trailing_stop_level:
                    self.BuyMarket()
                    self._reset_series()

    def _get_price_step(self):
        step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.0001
        return step

    def _reset_series(self):
        self._direction = 0
        self._entries_in_series = 0
        self._last_entry_price = 0.0
        self._average_entry_price = 0.0
        self._trailing_stop_level = None

    def CreateClone(self):
        return dealers_trade_v751_rivot_strategy()
