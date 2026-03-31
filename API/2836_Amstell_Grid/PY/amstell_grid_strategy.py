import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class amstell_grid_strategy(Strategy):
    """
    Grid strategy that alternates buy and sell entries with a virtual take profit.
    """

    def __init__(self):
        super(amstell_grid_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Virtual take profit distance", "Risk")
        self._step_pips = self.Param("StepPips", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (pips)", "Distance between grid entries", "Grid")
        self._candle_type = self.Param("CandleType", tf(240)) \
            .SetDisplay("Candle Type", "Timeframe for signal candles", "General")

        self._long_entries = []
        self._short_entries = []
        self._last_buy_price = None
        self._last_sell_price = None
        self._has_initial_order = False

    @property
    def TakeProfitPips(self): return self._take_profit_pips.Value
    @TakeProfitPips.setter
    def TakeProfitPips(self, v): self._take_profit_pips.Value = v
    @property
    def StepPips(self): return self._step_pips.Value
    @StepPips.setter
    def StepPips(self, v): self._step_pips.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(amstell_grid_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._last_buy_price = None
        self._last_sell_price = None
        self._has_initial_order = False

    def OnStarted2(self, time):
        super(amstell_grid_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        step_dist = self.StepPips * 1.0
        tp_dist = self.TakeProfitPips * 1.0

        if not self._has_initial_order and self._last_buy_price is None and self._last_sell_price is None:
            self.BuyMarket(self.Volume)
            self._has_initial_order = True
            return

        # Check grid buy
        if self._last_buy_price is None or self._last_buy_price - price >= step_dist:
            self.BuyMarket(self.Volume)
            self._last_buy_price = price
            return

        # Check grid sell
        if self._last_sell_price is None or price - self._last_sell_price >= step_dist:
            self.SellMarket(self.Volume)
            self._last_sell_price = price
            return

        # Check TP for longs
        for entry in self._long_entries:
            if not entry.get("closing", False) and price - entry["price"] >= tp_dist:
                entry["closing"] = True
                self.SellMarket(entry["volume"])
                return

        # Check TP for shorts
        for entry in self._short_entries:
            if not entry.get("closing", False) and entry["price"] - price >= tp_dist:
                entry["closing"] = True
                self.BuyMarket(entry["volume"])
                return

    def OnOwnTradeReceived(self, trade):
        super(amstell_grid_strategy, self).OnOwnTradeReceived(trade)
        price = float(trade.Trade.Price)
        volume = float(trade.Trade.Volume)
        side = trade.Order.Side

        if side == Sides.Buy:
            remainder = self._reduce_entries(self._short_entries, volume)
            if remainder > 0:
                self._long_entries.append({"price": price, "volume": remainder, "closing": False})
                self._last_buy_price = price
        elif side == Sides.Sell:
            remainder = self._reduce_entries(self._long_entries, volume)
            if remainder > 0:
                self._short_entries.append({"price": price, "volume": remainder, "closing": False})
                self._last_sell_price = price

        self._update_last_prices()

    def _reduce_entries(self, entries, volume):
        remaining = volume
        while remaining > 0 and len(entries) > 0:
            entry = entries[0]
            used = min(entry["volume"], remaining)
            entry["volume"] -= used
            remaining -= used
            if entry["volume"] <= 0:
                entries.pop(0)
            else:
                entry["closing"] = False
        return remaining

    def _update_last_prices(self):
        if len(self._long_entries) == 0 and len(self._short_entries) > 0:
            self._last_buy_price = None
        if len(self._short_entries) == 0 and len(self._long_entries) > 0:
            self._last_sell_price = None
        for e in self._long_entries:
            e["closing"] = False
        for e in self._short_entries:
            e["closing"] = False

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return amstell_grid_strategy()
