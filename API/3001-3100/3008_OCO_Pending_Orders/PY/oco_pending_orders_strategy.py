import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Messages import DataType, Level1Fields, Sides, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class oco_pending_orders_strategy(Strategy):
    def __init__(self):
        super(oco_pending_orders_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0).SetGreaterThanZero()
        self._buy_limit = self.Param("BuyLimitPrice", 0.0).SetNotNegative()
        self._buy_stop = self.Param("BuyStopPrice", 0.0).SetNotNegative()
        self._sell_limit = self.Param("SellLimitPrice", 0.0).SetNotNegative()
        self._sell_stop = self.Param("SellStopPrice", 0.0).SetNotNegative()
        self._stop_loss = self.Param("StopLossPips", 0).SetNotNegative()
        self._take_profit = self.Param("TakeProfitPips", 0).SetNotNegative()
        self._use_oco = self.Param("UseOcoLink", True)
        self._armed = self.Param("Armed", False)

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(oco_pending_orders_strategy, self).OnStarted2(time)

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        tp = int(self._take_profit.Value)
        sl = int(self._stop_loss.Value)
        take = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else None
        stop = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else None

        if take is not None or stop is not None:
            self.StartProtection(takeProfit=take, stopLoss=stop, useMarketOrders=True)

        self.SubscribeLevel1().Bind(self._process_level1).Start()

    def _process_level1(self, message):
        if not bool(self._armed.Value):
            return

        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)

        if ask is not None:
            best_ask = float(ask)
            if float(self._buy_limit.Value) > 0 and best_ask <= float(self._buy_limit.Value):
                self._buy_limit.Value = 0.0
                self._execute(Sides.Buy)
                return
            if float(self._buy_stop.Value) > 0 and best_ask >= float(self._buy_stop.Value):
                self._buy_stop.Value = 0.0
                self._execute(Sides.Buy)
                return

        if bid is not None:
            best_bid = float(bid)
            if float(self._sell_limit.Value) > 0 and best_bid >= float(self._sell_limit.Value):
                self._sell_limit.Value = 0.0
                self._execute(Sides.Sell)
                return
            if float(self._sell_stop.Value) > 0 and best_bid <= float(self._sell_stop.Value):
                self._sell_stop.Value = 0.0
                self._execute(Sides.Sell)
                return

        self._disarm_if_empty()

    def _execute(self, side):
        volume = float(self._order_volume.Value)
        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        if bool(self._use_oco.Value):
            self._clear_levels()
            self._armed.Value = False
        else:
            self._disarm_if_empty()

    def _clear_levels(self):
        self._buy_limit.Value = 0.0
        self._buy_stop.Value = 0.0
        self._sell_limit.Value = 0.0
        self._sell_stop.Value = 0.0

    def _disarm_if_empty(self):
        if (float(self._buy_limit.Value) <= 0 and float(self._buy_stop.Value) <= 0 and
                float(self._sell_limit.Value) <= 0 and float(self._sell_stop.Value) <= 0):
            self._armed.Value = False

    def CreateClone(self):
        return oco_pending_orders_strategy()
