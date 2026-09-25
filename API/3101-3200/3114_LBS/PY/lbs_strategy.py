import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.MatchingEngine")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides, OrderTypes, OrderStates
from StockSharp.BusinessEntities import Order
from StockSharp.MatchingEngine import StopOrderCondition
from StockSharp.Algo.Strategies import Strategy


class lbs_strategy(Strategy):
    FIXED_LOT = 0
    RISK_PERCENT = 1

    def __init__(self):
        super(lbs_strategy, self).__init__()

        self._stop_loss = self.Param("StopLossPips", 50).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStopPips", 5).SetNotNegative()
        self._trailing_step = self.Param("TrailingStepPips", 15).SetNotNegative()
        self._money_mode = self.Param("MoneyMode", self.FIXED_LOT)
        self._volume_or_risk = self.Param("VolumeOrRisk", 1.0).SetGreaterThanZero()
        self._hour1 = self.Param("Hour1", 10).SetRange(0, 23)
        self._hour2 = self.Param("Hour2", 11).SetRange(0, 23)
        self._hour3 = self.Param("Hour3", 12).SetRange(0, 23)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._bid = None
        self._ask = None
        self._buy_pending = None
        self._sell_pending = None
        self._protective_stop = None
        self._entry_price = 0.0

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(lbs_strategy, self).OnReseted()
        self._bid = None
        self._ask = None
        self._buy_pending = None
        self._sell_pending = None
        self._protective_stop = None
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(lbs_strategy, self).OnStarted2(time)
        self.SubscribeLevel1().Bind(self._process_level1).Start()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished or self.Position != 0:
            return

        hour = candle.CloseTime.Hour
        if not self._is_trading_hour(hour) or self._bid is None or self._ask is None:
            return

        self._cancel_entry_stops()

        point = self._point()
        buy_price, sell_price = self.calculate_breakout_levels(
            float(candle.HighPrice), float(candle.LowPrice), self._bid, self._ask, point)

        volume = self._calculate_volume(point)
        if volume <= 0:
            return

        self._buy_pending = self._create_stop(Sides.Buy, buy_price, volume, "LBS buy breakout")
        self._sell_pending = self._create_stop(Sides.Sell, sell_price, volume, "LBS sell breakout")
        self.RegisterOrder(self._buy_pending)
        self.RegisterOrder(self._sell_pending)

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None and float(bid) > 0:
            self._bid = float(bid)
        if ask is not None and float(ask) > 0:
            self._ask = float(ask)

        if (self.Position == 0 or self._protective_stop is None or
                int(self._trailing_stop.Value) <= 0 or int(self._trailing_step.Value) <= 0):
            return

        point = self._point()
        trail = int(self._trailing_stop.Value) * point
        step = int(self._trailing_step.Value) * point
        current = self._activation(self._protective_stop)

        if self.Position > 0 and self._bid is not None:
            if self._bid - self._entry_price < trail + step:
                return
            candidate = self._bid - trail
            if candidate >= current + step:
                self._replace_protective(Sides.Sell, candidate)
        elif self.Position < 0 and self._ask is not None:
            if self._entry_price - self._ask < trail + step:
                return
            candidate = self._ask + trail
            if candidate <= current - step:
                self._replace_protective(Sides.Buy, candidate)

    def OnOwnTradeReceived(self, trade):
        super(lbs_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None or trade.Trade is None:
            return

        if self._same(trade.Order, self._buy_pending) or self._same(trade.Order, self._sell_pending):
            side = trade.Order.Side
            self._entry_price = float(trade.Trade.TradePrice)

            if side == Sides.Buy:
                self._cancel_if_active(self._sell_pending)
            else:
                self._cancel_if_active(self._buy_pending)

            self._buy_pending = None
            self._sell_pending = None

            sl = int(self._stop_loss.Value)
            if sl > 0 and self.Position != 0:
                point = self._point()
                stop = self._entry_price - sl * point if side == Sides.Buy else self._entry_price + sl * point
                self._replace_protective(Sides.Sell if side == Sides.Buy else Sides.Buy, stop)
            return

        if self._same(trade.Order, self._protective_stop) and self.Position == 0:
            self._protective_stop = None
            self._entry_price = 0.0

    def _is_trading_hour(self, hour):
        return ((int(self._hour1.Value) != 0 and hour == int(self._hour1.Value)) or
                (int(self._hour2.Value) != 0 and hour == int(self._hour2.Value)) or
                (int(self._hour3.Value) != 0 and hour == int(self._hour3.Value)))

    def _calculate_volume(self, point):
        if int(self._money_mode.Value) == self.FIXED_LOT:
            return self._normalize_volume(float(self._volume_or_risk.Value))

        sl = int(self._stop_loss.Value)
        if sl <= 0:
            return 0.0

        balance = 0.0
        if self.Portfolio is not None:
            value = self.Portfolio.CurrentValue if self.Portfolio.CurrentValue is not None else self.Portfolio.BeginValue
            balance = float(value) if value is not None else 0.0

        if balance <= 0:
            return 0.0

        risk_money = balance * float(self._volume_or_risk.Value) / 100.0
        step_price = float(self.Security.StepPrice) if self.Security is not None and self.Security.StepPrice is not None else 0.0
        multiplier = float(self.Security.Multiplier) if self.Security is not None and self.Security.Multiplier is not None else 1.0
        loss_per_unit = sl * step_price if step_price > 0 else sl * point * multiplier
        return self._normalize_volume(risk_money / loss_per_unit) if loss_per_unit > 0 else 0.0

    def _create_stop(self, side, activation, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Conditional
        condition = StopOrderCondition()
        condition.ActivationPrice = self.Security.ShrinkPrice(activation)
        order.Condition = condition
        order.Side = side
        order.Volume = volume
        order.Comment = comment
        return order

    def _replace_protective(self, side, activation):
        volume = abs(float(self.Position))
        if volume <= 0:
            return

        replacement = self._create_stop(side, activation, volume, "LBS protective stop")
        old = self._protective_stop
        if old is not None and old.State == OrderStates.Active:
            self.ReRegisterOrder(old, replacement)
        else:
            self.RegisterOrder(replacement)
        self._protective_stop = replacement

    def _cancel_entry_stops(self):
        self._cancel_if_active(self._buy_pending)
        self._cancel_if_active(self._sell_pending)
        self._buy_pending = None
        self._sell_pending = None

    def _cancel_if_active(self, order):
        if order is not None and order.State == OrderStates.Active:
            self.CancelOrder(order)

    @staticmethod
    def _same(left, right):
        return left is not None and right is not None and (
            left is right or left.TransactionId == right.TransactionId)

    @staticmethod
    def _activation(order):
        if order is None or order.Condition is None:
            return 0.0
        return float(order.Condition.ActivationPrice) if order.Condition.ActivationPrice is not None else 0.0

    def _normalize_volume(self, volume):
        if self.Security is not None:
            if self.Security.MaxVolume is not None and float(self.Security.MaxVolume) > 0:
                volume = min(volume, float(self.Security.MaxVolume))
            if self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
                volume = max(volume, float(self.Security.MinVolume))
            if self.Security.VolumeStep is not None and float(self.Security.VolumeStep) > 0:
                step = float(self.Security.VolumeStep)
                volume = math.floor(volume / step) * step
        return volume

    def _point(self):
        point = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        return point if point > 0 else 0.0001

    @staticmethod
    def calculate_breakout_levels(candle_high, candle_low, bid, ask, price_step):
        spread = max(0.0, ask - bid)
        buffer = max(3.0 * spread, 10.0 * price_step)
        return max(candle_high, ask + buffer), min(candle_low, bid - buffer)

    def CreateClone(self):
        return lbs_strategy()
