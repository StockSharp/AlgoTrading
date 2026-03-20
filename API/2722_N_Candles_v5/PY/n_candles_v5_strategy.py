import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class n_candles_v5_strategy(Strategy):
    """N Candles v5: trades after N consecutive same-direction candles with SL/TP/trailing."""

    def __init__(self):
        super(n_candles_v5_strategy, self).__init__()

        self._volume_param = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._candles_count = self.Param("CandlesCount", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Candles Count", "Number of identical candles required", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop activation distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 4.0) \
            .SetDisplay("Trailing Step (pips)", "Increment required to tighten trailing stop", "Risk")
        self._use_trading_hours = self.Param("UseTradingHours", True) \
            .SetDisplay("Use Trading Hours", "Enable trading session filter", "Trading")
        self._start_hour = self.Param("StartHour", 11) \
            .SetDisplay("Start Hour", "Hour when trading is allowed to start", "Trading")
        self._end_hour = self.Param("EndHour", 18) \
            .SetDisplay("End Hour", "Hour when trading is allowed to stop", "Trading")
        self._max_net_volume = self.Param("MaxNetVolume", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Net Volume", "Maximum absolute net position", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")

        self._bullish_count = 0
        self._bearish_count = 0
        self._long_entry_price = None
        self._long_take_profit = None
        self._long_stop_loss = None
        self._long_trailing_stop = None
        self._short_entry_price = None
        self._short_take_profit = None
        self._short_stop_loss = None
        self._short_trailing_stop = None

    @property
    def TradeVolume(self):
        return float(self._volume_param.Value)
    @property
    def CandlesCount(self):
        return int(self._candles_count.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def TrailingStepPips(self):
        return float(self._trailing_step_pips.Value)
    @property
    def UseTradingHours(self):
        return self._use_trading_hours.Value
    @property
    def StartHour(self):
        return int(self._start_hour.Value)
    @property
    def EndHour(self):
        return int(self._end_hour.Value)
    @property
    def MaxNetVolume(self):
        return float(self._max_net_volume.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(n_candles_v5_strategy, self).OnStarted(time)

        self._bullish_count = 0
        self._bearish_count = 0
        self._clear_long_state()
        self._clear_short_state()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        self._update_risk_management(candle)

        # Determine direction
        if close > open_p:
            direction = 1
        elif close < open_p:
            direction = -1
        else:
            direction = 0

        if direction == 1:
            self._bullish_count += 1
            self._bearish_count = 0
        elif direction == -1:
            self._bearish_count += 1
            self._bullish_count = 0
        else:
            self._bullish_count = 0
            self._bearish_count = 0

        trading_allowed = not self.UseTradingHours or (candle.OpenTime.Hour >= self.StartHour and candle.OpenTime.Hour <= self.EndHour)
        if not trading_allowed:
            return

        volume = self.TradeVolume
        if volume <= 0:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0

        if self._bullish_count >= self.CandlesCount and self.Position <= 0:
            order_volume = volume + max(0.0, -self.Position)
            if order_volume > 0 and abs(self.Position + order_volume) <= self.MaxNetVolume:
                self.BuyMarket()
                self._setup_long_state(candle, step)
            self._reset_counters()
        elif self._bearish_count >= self.CandlesCount and self.Position >= 0:
            order_volume = volume + max(0.0, self.Position)
            if order_volume > 0 and abs(self.Position - order_volume) <= self.MaxNetVolume:
                self.SellMarket()
                self._setup_short_state(candle, step)
            self._reset_counters()

    def _update_risk_management(self, candle):
        if self.Position > 0:
            self._manage_long_position(candle)
        else:
            self._clear_long_state()

        if self.Position < 0:
            self._manage_short_position(candle)
        else:
            self._clear_short_state()

    def _manage_long_position(self, candle):
        if self._long_entry_price is None:
            self._long_entry_price = float(candle.ClosePrice)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        close = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        trailing_distance = self.TrailingStopPips * step if self.TrailingStopPips > 0 else 0.0
        trailing_step = self.TrailingStepPips * step if self.TrailingStepPips > 0 else 0.0

        if self.TrailingStopPips > 0 and self._long_entry_price is not None:
            entry = self._long_entry_price
            if self._long_trailing_stop is None:
                if close - trailing_distance > entry:
                    self._long_trailing_stop = entry
            else:
                new_level = close - trailing_distance
                if new_level - trailing_step > self._long_trailing_stop:
                    self._long_trailing_stop = new_level
        else:
            self._long_trailing_stop = None

        closed = False

        if not closed and self._long_take_profit is not None and h >= self._long_take_profit:
            if self.Position > 0:
                self.SellMarket()
            closed = True

        if not closed and self._long_stop_loss is not None and lo <= self._long_stop_loss:
            if self.Position > 0:
                self.SellMarket()
            closed = True

        if not closed and self._long_trailing_stop is not None and lo <= self._long_trailing_stop:
            if self.Position > 0:
                self.SellMarket()
            closed = True

        if closed:
            self._clear_long_state()

    def _manage_short_position(self, candle):
        if self._short_entry_price is None:
            self._short_entry_price = float(candle.ClosePrice)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        close = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        trailing_distance = self.TrailingStopPips * step if self.TrailingStopPips > 0 else 0.0
        trailing_step = self.TrailingStepPips * step if self.TrailingStepPips > 0 else 0.0

        if self.TrailingStopPips > 0 and self._short_entry_price is not None:
            entry = self._short_entry_price
            if self._short_trailing_stop is None:
                if close + trailing_distance < entry:
                    self._short_trailing_stop = entry
            else:
                new_level = close + trailing_distance
                if new_level + trailing_step < self._short_trailing_stop:
                    self._short_trailing_stop = new_level
        else:
            self._short_trailing_stop = None

        closed = False

        if not closed and self._short_take_profit is not None and lo <= self._short_take_profit:
            if self.Position < 0:
                self.BuyMarket()
            closed = True

        if not closed and self._short_stop_loss is not None and h >= self._short_stop_loss:
            if self.Position < 0:
                self.BuyMarket()
            closed = True

        if not closed and self._short_trailing_stop is not None and h >= self._short_trailing_stop:
            if self.Position < 0:
                self.BuyMarket()
            closed = True

        if closed:
            self._clear_short_state()

    def _setup_long_state(self, candle, step):
        entry_price = float(candle.ClosePrice)
        self._long_entry_price = entry_price
        self._long_take_profit = entry_price + self.TakeProfitPips * step if self.TakeProfitPips > 0 else None
        self._long_stop_loss = entry_price - self.StopLossPips * step if self.StopLossPips > 0 else None
        self._long_trailing_stop = None
        self._clear_short_state()

    def _setup_short_state(self, candle, step):
        entry_price = float(candle.ClosePrice)
        self._short_entry_price = entry_price
        self._short_take_profit = entry_price - self.TakeProfitPips * step if self.TakeProfitPips > 0 else None
        self._short_stop_loss = entry_price + self.StopLossPips * step if self.StopLossPips > 0 else None
        self._short_trailing_stop = None
        self._clear_long_state()

    def _clear_long_state(self):
        self._long_entry_price = None
        self._long_take_profit = None
        self._long_stop_loss = None
        self._long_trailing_stop = None

    def _clear_short_state(self):
        self._short_entry_price = None
        self._short_take_profit = None
        self._short_stop_loss = None
        self._short_trailing_stop = None

    def _reset_counters(self):
        self._bullish_count = 0
        self._bearish_count = 0

    def OnReseted(self):
        super(n_candles_v5_strategy, self).OnReseted()
        self._bullish_count = 0
        self._bearish_count = 0
        self._clear_long_state()
        self._clear_short_state()

    def CreateClone(self):
        return n_candles_v5_strategy()
