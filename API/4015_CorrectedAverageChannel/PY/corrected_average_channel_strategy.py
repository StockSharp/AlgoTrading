import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    StandardDeviation,
)

class corrected_average_channel_strategy(Strategy):
    def __init__(self):
        super(corrected_average_channel_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Market order size used for entries", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 60) \
            .SetDisplay("Take Profit (points)", "Distance from entry to the profit target in price steps", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 40) \
            .SetDisplay("Stop Loss (points)", "Distance from entry to the protective stop in price steps", "Risk")
        self._trailing_points = self.Param("TrailingPoints", 0) \
            .SetDisplay("Trailing Trigger (points)", "Profit distance required before the trailing stop activates", "Risk")
        self._trailing_step_points = self.Param("TrailingStepPoints", 0) \
            .SetDisplay("Trailing Step (points)", "Minimum advance in price steps before the trailing stop moves", "Risk")
        self._ma_period = self.Param("MaPeriod", 35) \
            .SetDisplay("MA Period", "Period of the moving average and standard deviation", "Indicator")
        self._ma_type = self.Param("MaType", 0) \
            .SetDisplay("MA Type", "0=SMA, 1=EMA, 2=SMMA, 3=LWMA", "Indicator")
        self._sigma_buy_points = self.Param("SigmaBuyPoints", 5) \
            .SetDisplay("Sigma BUY (points)", "Offset added above the corrected average before buying", "Signal")
        self._sigma_sell_points = self.Param("SigmaSellPoints", 5) \
            .SetDisplay("Sigma SELL (points)", "Offset subtracted from the corrected average before selling", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "Data")

        self._ma = None
        self._std = None
        self._price_step = 0.0
        self._sigma_buy_offset = 0.0
        self._sigma_sell_offset = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_distance = 0.0
        self._trailing_step_distance = 0.0
        self._previous_corrected = None
        self._previous_close = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None
        self._last_trade_side = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingPoints(self):
        return self._trailing_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def MaType(self):
        return self._ma_type.Value

    @property
    def SigmaBuyPoints(self):
        return self._sigma_buy_points.Value

    @property
    def SigmaSellPoints(self):
        return self._sigma_sell_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _create_ma(self, ma_type, length):
        if ma_type == 1:
            ind = ExponentialMovingAverage()
        elif ma_type == 2:
            ind = SmoothedMovingAverage()
        elif ma_type == 3:
            ind = WeightedMovingAverage()
        else:
            ind = SimpleMovingAverage()
        ind.Length = length
        return ind

    def _get_price_offset(self, points):
        pts = int(points)
        if pts <= 0 or self._price_step <= 0:
            return 0.0
        return pts * self._price_step

    def OnStarted2(self, time):
        super(corrected_average_channel_strategy, self).OnStarted2(time)

        self._ma = self._create_ma(self.MaType, self.MaPeriod)
        self._std = StandardDeviation()
        self._std.Length = self.MaPeriod

        self._price_step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            self._price_step = float(self.Security.PriceStep)
        if self._price_step <= 0:
            self._price_step = 1.0

        self._sigma_buy_offset = self._get_price_offset(self.SigmaBuyPoints)
        self._sigma_sell_offset = self._get_price_offset(self.SigmaSellPoints)
        self._stop_loss_distance = self._get_price_offset(self.StopLossPoints)
        self._take_profit_distance = self._get_price_offset(self.TakeProfitPoints)
        self._trailing_distance = self._get_price_offset(self.TrailingPoints)
        self._trailing_step_distance = self._get_price_offset(self.TrailingStepPoints)

        self.Volume = float(self.OrderVolume)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ma, self._std, self.ProcessCandle).Start()

    def OnOwnTradeReceived(self, trade):
        super(corrected_average_channel_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None:
            return

        if trade.Trade is not None:
            self._last_trade_price = float(trade.Trade.Price)

        self._last_trade_side = trade.Order.Side

        prev_pos = self._previous_position
        cur_pos = self.Position

        if prev_pos == 0 and cur_pos != 0:
            entry_price = self._last_trade_price if self._last_trade_price is not None else self._previous_close
            if entry_price is not None:
                if cur_pos > 0 and self._last_trade_side == Sides.Buy:
                    self._initialize_risk_state(entry_price, True)
                elif cur_pos < 0 and self._last_trade_side == Sides.Sell:
                    self._initialize_risk_state(entry_price, False)
        elif cur_pos == 0 and prev_pos != 0:
            self._reset_risk_state()

        self._previous_position = cur_pos

    def ProcessCandle(self, candle, ma_value, std_value):
        if candle.State != CandleStates.Finished:
            return

        ma_value = float(ma_value)
        std_value = float(std_value)

        if self._ma is None or self._std is None:
            return

        if not self._ma.IsFormed or not self._std.IsFormed:
            self._previous_corrected = ma_value
            self._previous_close = float(candle.ClosePrice)
            return

        previous_corrected = self._previous_corrected
        previous_close = self._previous_close

        if previous_corrected is None:
            corrected = ma_value
        else:
            diff = previous_corrected - ma_value
            v2 = diff * diff
            v1 = std_value * std_value
            if v2 <= 0 or v2 < v1:
                k = 0.0
            else:
                k = 1.0 - (v1 / v2)
            corrected = previous_corrected + k * (ma_value - previous_corrected)

        if self._handle_trailing(candle):
            self._previous_corrected = corrected
            self._previous_close = float(candle.ClosePrice)
            return

        if self._handle_risk_exit(candle):
            self._previous_corrected = corrected
            self._previous_close = float(candle.ClosePrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_corrected = corrected
            self._previous_close = float(candle.ClosePrice)
            return

        if self.Position == 0 and previous_corrected is not None and previous_close is not None:
            buy_threshold = corrected + self._sigma_buy_offset
            sell_threshold = corrected - self._sigma_sell_offset
            close_price = float(candle.ClosePrice)

            buy_signal = previous_close < previous_corrected + self._sigma_buy_offset and close_price >= buy_threshold
            sell_signal = previous_close > previous_corrected - self._sigma_sell_offset and close_price <= sell_threshold

            if buy_signal:
                self.BuyMarket()
            elif sell_signal:
                self.SellMarket()

        self._previous_corrected = corrected
        self._previous_close = float(candle.ClosePrice)

    def _handle_trailing(self, candle):
        if self._trailing_distance <= 0 or self._entry_price is None:
            return False

        volume = abs(self.Position)
        if volume <= 0:
            return False

        close_price = float(candle.ClosePrice)

        if self.Position > 0:
            moved = close_price - self._entry_price
            if moved > self._trailing_distance:
                candidate = close_price - self._trailing_distance
                if self._long_trailing_stop is None or candidate - self._long_trailing_stop >= self._trailing_step_distance:
                    self._long_trailing_stop = candidate

            if self._long_trailing_stop is not None and float(candle.LowPrice) <= self._long_trailing_stop:
                self.SellMarket(volume)
                self._reset_risk_state()
                return True

        elif self.Position < 0:
            moved = self._entry_price - close_price
            if moved > self._trailing_distance:
                candidate = close_price + self._trailing_distance
                if self._short_trailing_stop is None or self._short_trailing_stop - candidate >= self._trailing_step_distance:
                    self._short_trailing_stop = candidate

            if self._short_trailing_stop is not None and float(candle.HighPrice) >= self._short_trailing_stop:
                self.BuyMarket(volume)
                self._reset_risk_state()
                return True

        return False

    def _handle_risk_exit(self, candle):
        volume = abs(self.Position)
        if volume <= 0:
            return False

        if self.Position > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(volume)
                self._reset_risk_state()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(volume)
                self._reset_risk_state()
                return True

        elif self.Position < 0:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(volume)
                self._reset_risk_state()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(volume)
                self._reset_risk_state()
                return True

        return False

    def _initialize_risk_state(self, entry_price, is_long):
        self._entry_price = entry_price
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None

        if self._stop_loss_distance > 0:
            if is_long:
                self._stop_loss_price = entry_price - self._stop_loss_distance
            else:
                self._stop_loss_price = entry_price + self._stop_loss_distance

        if self._take_profit_distance > 0:
            if is_long:
                self._take_profit_price = entry_price + self._take_profit_distance
            else:
                self._take_profit_price = entry_price - self._take_profit_distance

    def _reset_risk_state(self):
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None

    def OnReseted(self):
        super(corrected_average_channel_strategy, self).OnReseted()
        self._ma = None
        self._std = None
        self._price_step = 0.0
        self._sigma_buy_offset = 0.0
        self._sigma_sell_offset = 0.0
        self._stop_loss_distance = 0.0
        self._take_profit_distance = 0.0
        self._trailing_distance = 0.0
        self._trailing_step_distance = 0.0
        self._previous_corrected = None
        self._previous_close = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._previous_position = 0.0
        self._last_trade_price = None
        self._last_trade_side = None

    def CreateClone(self):
        return corrected_average_channel_strategy()
