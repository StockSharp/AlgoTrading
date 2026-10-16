import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Convert
from StockSharp.Messages import DataType, CandleStates, OrderStates, Level1Fields, Sides
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class super_take_strategy(Strategy):
    """Alternating trades with fill-based Labouchere-free take-profit martingale.

    Candle closes schedule entries ONLY. Protection triggers on fresh executable
    bid/ask quotes after a real fill; pending or partially filled orders do not
    create imaginary closes or permit duplicate entries/exits.
    """

    def __init__(self):
        super(super_take_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 10.0).SetDisplay(
            "Take Profit", "Base price-distance target", "Risk")
        self._stop_loss = self.Param("StopLoss", 15.0).SetDisplay(
            "Stop Loss", "Price-distance stop", "Risk")
        self._martin_factor = self.Param("MartinFactor", 1.8).SetDisplay(
            "Martingale Factor", "Take-profit distance multiplier after loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay(
            "Candle Type", "Type of candles", "General")
        self._reset_state()
        self.OrderRegistering += self._track_order

    def _track_order(self, order):
        if self._submitting_entry:
            self._entry_order = order
        elif self._submitting_exit:
            self._exit_order = order
        self._pending_order = order

    @property
    def TakeProfit(self): return self._take_profit.Value
    @TakeProfit.setter
    def TakeProfit(self, value): self._take_profit.Value = value

    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, value): self._stop_loss.Value = value

    @property
    def MartinFactor(self): return self._martin_factor.Value
    @MartinFactor.setter
    def MartinFactor(self, value): self._martin_factor.Value = value

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def _reset_state(self):
        zero = Convert.ToDecimal(0)
        self._submitting_entry = False
        self._submitting_exit = False
        self._entry_order = None
        self._exit_order = None
        self._pending_order = None
        self._entry_long = True
        self._entry_volume = zero
        self._entry_value = zero
        self._remaining_volume = zero
        self._realized_pnl = zero
        self._current_take_profit = zero
        self._last_take_profit_distance = zero
        self._last_closed_was_buy = None
        self._last_trade_was_loss = False

    def OnReseted(self):
        super(super_take_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(super_take_strategy, self).OnStarted2(time)
        self._reset_state()
        self._last_take_profit_distance = Convert.ToDecimal(self.TakeProfit)
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            sub = Subscription(DataType.Level1, self.Security)
            sub.MarketData.BuildField = field
            self.SubscribeLevel1(sub).Bind(self._process_quote).Start()

    def _has_pending(self):
        return self._pending_order is not None and self._pending_order.State not in (
            OrderStates.Done, OrderStates.Failed)

    def ProcessCandle(self, candle):
        if (candle.State != CandleStates.Finished or self.Position != 0 or
                self._remaining_volume != 0 or self._has_pending() or
                not self.IsFormedAndOnlineAndAllowTrading()):
            return

        self._entry_long = self._last_closed_was_buy is not True
        base = Convert.ToDecimal(self.TakeProfit)
        self._current_take_profit = (self._last_take_profit_distance * Convert.ToDecimal(self.MartinFactor)
                                     if self._last_trade_was_loss else base)
        self._entry_volume = Convert.ToDecimal(0)
        self._entry_value = Convert.ToDecimal(0)
        self._realized_pnl = Convert.ToDecimal(0)
        self._exit_order = None
        self._submitting_entry = True
        try:
            self._entry_order = self.BuyMarket(self.Volume) if self._entry_long else self.SellMarket(self.Volume)
            self._pending_order = self._entry_order
        finally:
            self._submitting_entry = False

    def OnOwnTradeReceived(self, trade):
        super(super_take_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order is None or trade.Trade is None:
            return
        volume = trade.Trade.Volume
        price = trade.Trade.Price
        if trade.Order == self._entry_order:
            self._entry_volume += volume
            self._entry_value += volume * price
            self._remaining_volume += volume
        elif trade.Order == self._exit_order and self._entry_volume > 0:
            entry = self._entry_value / self._entry_volume
            self._realized_pnl += (price - entry if self._entry_long else entry - price) * volume
            self._remaining_volume = max(Convert.ToDecimal(0), self._remaining_volume - volume)
            if self._remaining_volume == 0:
                self._last_closed_was_buy = self._entry_long
                self._last_trade_was_loss = self._realized_pnl < 0
                self._last_take_profit_distance = self._current_take_profit

    def _process_quote(self, quote):
        if (self.Position == 0 or self._remaining_volume <= 0 or self._entry_volume <= 0 or
                self._has_pending() or not self.IsFormedAndOnlineAndAllowTrading()):
            return
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Convert.ToDecimal(quote.Changes[field])
        if price <= 0:
            return
        entry = self._entry_value / self._entry_volume
        profit = price - entry if self.Position > 0 else entry - price
        if profit >= self._current_take_profit or profit <= -Convert.ToDecimal(self.StopLoss):
            volume = abs(self.Position)
            self._submitting_exit = True
            try:
                self._exit_order = self.SellMarket(volume) if self.Position > 0 else self.BuyMarket(volume)
                self._pending_order = self._exit_order
            finally:
                self._submitting_exit = False

    def CreateClone(self):
        return super_take_strategy()
