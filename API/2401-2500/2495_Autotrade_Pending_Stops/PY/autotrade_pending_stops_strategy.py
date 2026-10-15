import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class autotrade_pending_stops_strategy(Strategy):
    def __init__(self):
        super(autotrade_pending_stops_strategy, self).__init__()

        self._indent_ticks = self.Param("IndentTicks", 200)
        self._min_profit = self.Param("MinProfit", 2.0)
        self._expiration_minutes = self.Param("ExpirationMinutes", 41)
        self._absolute_fixation = self.Param("AbsoluteFixation", 43.0)
        self._stabilization_ticks = self.Param("StabilizationTicks", 25)
        self._order_volume = self.Param("OrderVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev_candle = False
        self._tick_size = 1.0
        self._entry_price = 0.0

    @property
    def IndentTicks(self):
        return self._indent_ticks.Value

    @IndentTicks.setter
    def IndentTicks(self, value):
        self._indent_ticks.Value = value

    @property
    def MinProfit(self):
        return self._min_profit.Value

    @MinProfit.setter
    def MinProfit(self, value):
        self._min_profit.Value = value

    @property
    def ExpirationMinutes(self):
        return self._expiration_minutes.Value

    @ExpirationMinutes.setter
    def ExpirationMinutes(self, value):
        self._expiration_minutes.Value = value

    @property
    def AbsoluteFixation(self):
        return self._absolute_fixation.Value

    @AbsoluteFixation.setter
    def AbsoluteFixation(self, value):
        self._absolute_fixation.Value = value

    @property
    def StabilizationTicks(self):
        return self._stabilization_ticks.Value

    @StabilizationTicks.setter
    def StabilizationTicks(self, value):
        self._stabilization_ticks.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(autotrade_pending_stops_strategy, self).OnStarted2(time)

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev_candle = False
        self._entry_price = 0.0

        self.Volume = self._order_volume.Value

        self._tick_size = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._tick_size <= 0.0:
            self._tick_size = 1.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)

        if not self._has_prev_candle:
            self._prev_open = open_price
            self._prev_close = close
            self._has_prev_candle = True
            self._ensure_pending_orders(candle)
            return

        if self.Position == 0:
            self._ensure_pending_orders(candle)
        else:
            self._manage_open_position(candle)

        self._prev_open = open_price
        self._prev_close = close

    def _ensure_pending_orders(self, candle):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        order_vol = float(self._order_volume.Value)

        indent = int(self._indent_ticks.Value) * self._tick_size
        buy_price = close + indent
        sell_price = close - indent

        pos = float(self.Position)
        if high >= buy_price and pos <= 0:
            if pos < 0:
                self.BuyMarket(abs(pos))
            self.BuyMarket(order_vol)
            self._entry_price = buy_price
        elif low <= sell_price and pos >= 0:
            if pos > 0:
                self.SellMarket(abs(pos))
            self.SellMarket(order_vol)
            self._entry_price = sell_price

    def _manage_open_position(self, candle):
        if self._entry_price == 0.0:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            price_diff = close - self._entry_price
        else:
            price_diff = self._entry_price - close

        prev_body_size = abs(self._prev_close - self._prev_open)
        exit_by_profit = price_diff > 0.0 and prev_body_size < close * 0.001
        exit_by_loss = price_diff < -close * 0.005

        if self.Position > 0 and (exit_by_profit or exit_by_loss):
            self.SellMarket()
        elif self.Position < 0 and (exit_by_profit or exit_by_loss):
            self.BuyMarket()

    def OnReseted(self):
        super(autotrade_pending_stops_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev_candle = False
        self._tick_size = 1.0
        self._entry_price = 0.0

    def CreateClone(self):
        return autotrade_pending_stops_strategy()
