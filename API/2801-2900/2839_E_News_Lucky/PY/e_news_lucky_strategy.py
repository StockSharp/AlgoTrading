import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class e_news_lucky_strategy(Strategy):
    def __init__(self):
        super(e_news_lucky_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._take_profit_pips = self.Param("TakeProfitPips", 150.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._distance_pips = self.Param("DistancePips", 20.0)
        self._placement_hour = self.Param("PlacementHour", 2)
        self._cancel_hour = self.Param("CancelHour", 22)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._pip_size = 0.0
        self._buy_level = None
        self._sell_level = None
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._pending_active = False
        self._last_was_placement_day = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(e_news_lucky_strategy, self).OnStarted2(time)

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        self._pip_size = step if step > 0 else 1.0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.CloseTime.Hour
        price = float(candle.ClosePrice)

        # Set breakout levels at placement hour
        if hour == self._placement_hour.Value and not self._last_was_placement_day and self.Position == 0:
            distance = self._distance_pips.Value * self._pip_size
            self._buy_level = price + distance
            self._sell_level = price - distance
            self._pending_active = True
            self._last_was_placement_day = True

        if hour != self._placement_hour.Value:
            self._last_was_placement_day = False

        # Cancel at cancel hour
        if hour == self._cancel_hour.Value and self._pending_active:
            self._pending_active = False
            self._buy_level = None
            self._sell_level = None

            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))

            self._entry_price = 0.0
            self._stop_price = None
            self._take_price = None
            return

        # Check breakout triggers
        if self._pending_active and self.Position == 0:
            if self._buy_level is not None and float(candle.HighPrice) >= self._buy_level:
                buy_level = self._buy_level
                self.BuyMarket(self.Volume)
                self._entry_price = buy_level
                self._stop_price = self._entry_price - self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
                self._take_price = self._entry_price + self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
                self._pending_active = False
                self._buy_level = None
                self._sell_level = None
            elif self._sell_level is not None and float(candle.LowPrice) <= self._sell_level:
                sell_level = self._sell_level
                self.SellMarket(self.Volume)
                self._entry_price = sell_level
                self._stop_price = self._entry_price + self._stop_loss_pips.Value * self._pip_size if self._stop_loss_pips.Value > 0 else None
                self._take_price = self._entry_price - self._take_profit_pips.Value * self._pip_size if self._take_profit_pips.Value > 0 else None
                self._pending_active = False
                self._buy_level = None
                self._sell_level = None

        # Manage open position
        if self.Position > 0:
            # Trailing stop for long
            if self._trailing_stop_pips.Value > 0 and self._entry_price > 0:
                trail_dist = self._trailing_stop_pips.Value * self._pip_size
                step_dist = self._trailing_step_pips.Value * self._pip_size
                if price - self._entry_price > trail_dist + step_dist:
                    new_stop = price - trail_dist
                    if self._stop_price is None or new_stop > self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_position()
                return

            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(self.Position)
                self._reset_position()

        elif self.Position < 0:
            # Trailing stop for short
            if self._trailing_stop_pips.Value > 0 and self._entry_price > 0:
                trail_dist = self._trailing_stop_pips.Value * self._pip_size
                step_dist = self._trailing_step_pips.Value * self._pip_size
                if self._entry_price - price > trail_dist + step_dist:
                    new_stop = price + trail_dist
                    if self._stop_price is None or new_stop < self._stop_price:
                        self._stop_price = new_stop

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_position()
                return

            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(self.Position))
                self._reset_position()

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

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

    def CreateClone(self):
        return e_news_lucky_strategy()
