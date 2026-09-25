import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.MatchingEngine")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes, OrderStates
from StockSharp.BusinessEntities import Order
from StockSharp.MatchingEngine import StopOrderCondition
from StockSharp.Algo.Strategies import Strategy


class martini_martingale_strategy(Strategy):
    def __init__(self):
        super(martini_martingale_strategy, self).__init__()

        self._step = self.Param("Step", 10.0).SetGreaterThanZero()
        self._profit_close = self.Param("ProfitClose", 10.0).SetGreaterThanZero()
        self._initial_volume = self.Param("InitialVolume", 0.1).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._buy_stop_order = None
        self._sell_stop_order = None
        self._last_execution_price = 0.0
        self._last_leg_volume = 0.0
        self._order_count = 0
        self._martingale_order_pending = False
        self._closing_cycle = False
        self._cycle_pnl_base = 0.0

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(martini_martingale_strategy, self).OnReseted()
        self._reset_cycle_state()

    def OnStarted2(self, time):
        super(martini_martingale_strategy, self).OnStarted2(time)
        self._reset_cycle_state()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0:
            if not self._closing_cycle and self._buy_stop_order is None and self._sell_stop_order is None:
                self._place_initial_stops(float(candle.ClosePrice))
            return

        if not self._closing_cycle and float(self.PnL) - self._cycle_pnl_base >= float(self._profit_close.Value):
            self._closing_cycle = True
            self._cancel_initial_stops()
            self._close_net_position()
            return

        if self._closing_cycle or self._martingale_order_pending or self._last_leg_volume <= 0 or self._order_count <= 0:
            return

        adverse = float(self._step.Value) * self._order_count
        if self.Position > 0 and float(candle.LowPrice) <= self._last_execution_price - adverse:
            self._place_martingale(Sides.Sell, self._last_leg_volume * 2.0)
        elif self.Position < 0 and float(candle.HighPrice) >= self._last_execution_price + adverse:
            self._place_martingale(Sides.Buy, self._last_leg_volume * 2.0)

    def _place_initial_stops(self, center):
        self._cycle_pnl_base = float(self.PnL)
        self._buy_stop_order = self._create_stop(Sides.Buy, center + float(self._step.Value), float(self._initial_volume.Value), "Martini initial buy stop")
        self._sell_stop_order = self._create_stop(Sides.Sell, center - float(self._step.Value), float(self._initial_volume.Value), "Martini initial sell stop")
        self.RegisterOrder(self._buy_stop_order)
        self.RegisterOrder(self._sell_stop_order)

    def _create_stop(self, side, activation, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Conditional
        condition = StopOrderCondition()
        condition.ActivationPrice = activation
        order.Condition = condition
        order.Side = side
        order.Volume = volume
        order.Comment = comment
        return order

    def _place_martingale(self, side, volume):
        volume = self._normalize_volume(volume)
        if volume <= 0:
            return
        self._martingale_order_pending = True
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

    def OnOwnTradeReceived(self, trade):
        super(martini_martingale_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None or trade.Trade is None:
            return

        if self._closing_cycle:
            if self.Position == 0:
                self._reset_cycle_state()
            return

        if trade.Order.Type == OrderTypes.Conditional:
            if trade.Order.Side == Sides.Buy:
                self._cancel_if_active(self._sell_stop_order)
            else:
                self._cancel_if_active(self._buy_stop_order)
            self._buy_stop_order = None
            self._sell_stop_order = None

        self._last_execution_price = float(trade.Trade.TradePrice)
        self._last_leg_volume = float(trade.Trade.Volume)
        self._order_count += 1
        self._martingale_order_pending = False

    def OnOrderChanged(self, order):
        super(martini_martingale_strategy, self).OnOrderChanged(order)
        if order is None or order.State != OrderStates.Done:
            return

        if self._buy_stop_order is not None and order.TransactionId == self._buy_stop_order.TransactionId and order.Balance == order.Volume:
            self._buy_stop_order = None
        if self._sell_stop_order is not None and order.TransactionId == self._sell_stop_order.TransactionId and order.Balance == order.Volume:
            self._sell_stop_order = None

    def _cancel_initial_stops(self):
        self._cancel_if_active(self._buy_stop_order)
        self._cancel_if_active(self._sell_stop_order)
        self._buy_stop_order = None
        self._sell_stop_order = None

    def _cancel_if_active(self, order):
        if order is not None and order.State == OrderStates.Active:
            self.CancelOrder(order)

    def _close_net_position(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

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

    def _reset_cycle_state(self):
        self._buy_stop_order = None
        self._sell_stop_order = None
        self._last_execution_price = 0.0
        self._last_leg_volume = 0.0
        self._order_count = 0
        self._martingale_order_pending = False
        self._closing_cycle = False
        self._cycle_pnl_base = float(self.PnL)

    def CreateClone(self):
        return martini_martingale_strategy()
