import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import Decimal
from StockSharp.Messages import DataType, Level1Fields, Sides, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription


class oco_pending_orders_strategy(Strategy):
    def __init__(self):
        super(oco_pending_orders_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order volume", "Volume sent with each market order", "Trading")
        self._buy_limit = self.Param("BuyLimitPrice", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Buy limit price", "Ask price threshold that activates a limit-style long entry, 0 disables", "Levels")
        self._buy_stop = self.Param("BuyStopPrice", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Buy stop price", "Ask price threshold that activates a stop-style long entry, 0 disables", "Levels")
        self._sell_limit = self.Param("SellLimitPrice", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Sell limit price", "Bid price threshold that activates a limit-style short entry, 0 disables", "Levels")
        self._sell_stop = self.Param("SellStopPrice", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Sell stop price", "Bid price threshold that activates a stop-style short entry, 0 disables", "Levels")
        self._stop_loss = self.Param("StopLossPips", 0) \
            .SetNotNegative() \
            .SetDisplay("Stop loss (pips)", "Protective stop distance in instrument points, multiplied by the price step", "Risk")
        self._take_profit = self.Param("TakeProfitPips", 0) \
            .SetNotNegative() \
            .SetDisplay("Take profit (pips)", "Profit target distance in instrument points, multiplied by the price step", "Risk")
        self._use_oco = self.Param("UseOcoLink", True) \
            .SetDisplay("Use OCO link", "The first filled order clears the remaining price levels and disarms the strategy", "Control")
        self._armed = self.Param("Armed", False) \
            .SetDisplay("Armed", "Safety switch for the triggers, reset to false when no active level remains", "Control")
        self._initial_levels = None
        self._initial_armed = False
        self._best_bid = None
        self._best_ask = None

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(oco_pending_orders_strategy, self).OnStarted2(time)
        self._initial_levels = [self._buy_limit.Value, self._buy_stop.Value, self._sell_limit.Value, self._sell_stop.Value]
        self._initial_armed = self._armed.Value

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        tp = int(self._take_profit.Value)
        sl = int(self._stop_loss.Value)
        take = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else None
        stop = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else None

        if take is not None or stop is not None:
            self.StartProtection(takeProfit=take, stopLoss=stop, useMarketOrders=True)

        # A Level1 binding only sees updates carrying its build field, so bid-only and ask-only updates need one binding each.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_level1).Start()

    def _process_level1(self, message):
        if message.Changes.ContainsKey(Level1Fields.BestBidPrice):
            self._best_bid = message.Changes[Level1Fields.BestBidPrice]
        if message.Changes.ContainsKey(Level1Fields.BestAskPrice):
            self._best_ask = message.Changes[Level1Fields.BestAskPrice]

        if not bool(self._armed.Value):
            return

        if self._best_ask is not None:
            best_ask = self._best_ask

            level = Decimal(float(self._buy_limit.Value))
            if level > 0 and best_ask <= level:
                self._buy_limit.Value = 0.0
                self._execute(Sides.Buy, "Buy limit {0} hit by ask {1}".format(level, best_ask))
                if not bool(self._armed.Value):
                    return

            level = Decimal(float(self._buy_stop.Value))
            if level > 0 and best_ask >= level:
                self._buy_stop.Value = 0.0
                self._execute(Sides.Buy, "Buy stop {0} hit by ask {1}".format(level, best_ask))
                if not bool(self._armed.Value):
                    return

        if self._best_bid is not None:
            best_bid = self._best_bid

            level = Decimal(float(self._sell_limit.Value))
            if level > 0 and best_bid >= level:
                self._sell_limit.Value = 0.0
                self._execute(Sides.Sell, "Sell limit {0} hit by bid {1}".format(level, best_bid))
                if not bool(self._armed.Value):
                    return

            level = Decimal(float(self._sell_stop.Value))
            if level > 0 and best_bid <= level:
                self._sell_stop.Value = 0.0
                self._execute(Sides.Sell, "Sell stop {0} hit by bid {1}".format(level, best_bid))
                if not bool(self._armed.Value):
                    return

        self._disarm_if_empty()

    def _execute(self, side, trigger):
        volume = float(self._order_volume.Value)
        self.LogInfo("{0}: {1} {2} at market.".format(trigger, "Buy" if side == Sides.Buy else "Sell", volume))

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        if not bool(self._use_oco.Value):
            return

        self._clear_levels()
        self._armed.Value = False
        self.LogInfo("OCO link cleared all trigger levels; strategy disarmed.")

    def _clear_levels(self):
        self._buy_limit.Value = 0.0
        self._buy_stop.Value = 0.0
        self._sell_limit.Value = 0.0
        self._sell_stop.Value = 0.0

    def _disarm_if_empty(self):
        if (float(self._buy_limit.Value) > 0 or float(self._buy_stop.Value) > 0 or
                float(self._sell_limit.Value) > 0 or float(self._sell_stop.Value) > 0):
            return

        self._armed.Value = False
        self.LogInfo("No trigger levels remain; strategy disarmed.")

    def OnReseted(self):
        super(oco_pending_orders_strategy, self).OnReseted()
        if self._initial_levels is not None:
            self._buy_limit.Value, self._buy_stop.Value, self._sell_limit.Value, self._sell_stop.Value = self._initial_levels
            self._armed.Value = self._initial_armed
        self._initial_levels = None
        self._initial_armed = False
        self._best_bid = None
        self._best_ask = None

    def CreateClone(self):
        return oco_pending_orders_strategy()
