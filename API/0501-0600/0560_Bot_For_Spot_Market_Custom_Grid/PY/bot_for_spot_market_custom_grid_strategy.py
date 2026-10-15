import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class bot_for_spot_market_custom_grid_strategy(Strategy):
    """
    Bot for Spot Market - Custom Grid strategy.
    Long only: buys an OrderValue worth of the asset as soon as it is flat, adds another OrderValue whenever the close drops
    NextEntryPercent below the last entry price, and sells the whole position once the close exceeds the average entry price
    by ProfitPercent.
    """

    def __init__(self):
        super(bot_for_spot_market_custom_grid_strategy, self).__init__()
        self._order_value = self.Param("OrderValue", 10.0).SetGreaterThanZero().SetDisplay("Order Value", "Value of each order in quote currency", "Parameters")
        self._min_amount_movement = self.Param("MinAmountMovement", 0.00001).SetNotNegative().SetDisplay("Min Amount Movement", "Smallest amount increment added to the rounded quantity", "Parameters")
        self._rounding = self.Param("Rounding", 5).SetNotNegative().SetDisplay("Rounding", "Decimal places the quantity is rounded to", "Parameters")
        self._next_entry_percent = self.Param("NextEntryPercent", 0.5).SetGreaterThanZero().SetDisplay("Next Entry Less Than (%)", "Drop below the last entry price that adds a new order", "Parameters")
        self._profit_percent = self.Param("ProfitPercent", 2.0).SetGreaterThanZero().SetDisplay("Profit (%)", "Rise above the average entry price that closes the position", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_grid()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_grid(self):
        self._last_entry_price = Decimal(0)
        self._avg_price = Decimal(0)
        self._bought_volume = Decimal(0)

    def OnReseted(self):
        super(bot_for_spot_market_custom_grid_strategy, self).OnReseted()
        self._reset_grid()

    def OnStarted2(self, time):
        super(bot_for_spot_market_custom_grid_strategy, self).OnStarted2(time)

        self._reset_grid()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = candle.ClosePrice
        hundred = Decimal(100)

        if self.Position <= 0:
            self.BuyMarket(self._get_quantity(price))
            return

        next_entry = Decimal(self._next_entry_percent.Value)
        if self._last_entry_price > 0 and price < self._last_entry_price * (Decimal(1) - next_entry / hundred):
            self.BuyMarket(self._get_quantity(price))
            return

        profit = Decimal(self._profit_percent.Value)
        if self._avg_price > 0 and price > self._avg_price * (Decimal(1) + profit / hundred):
            self.SellMarket(self.Position)

    def _get_quantity(self, price):
        min_move = Decimal(self._min_amount_movement.Value)
        raw = Decimal(self._order_value.Value) / price
        rounded = Math.Round(raw, int(self._rounding.Value))
        quantity = rounded + min_move if rounded >= raw else rounded + min_move * Decimal(2)

        # The instrument cannot accept less than its minimum volume or a fraction of its volume step.
        security = self.Security
        if security is not None:
            step = security.VolumeStep
            if step is not None and step > 0:
                quantity = Math.Ceiling(quantity / step) * step
            min_volume = security.MinVolume
            if min_volume is not None and quantity < min_volume:
                quantity = min_volume

        return quantity

    def OnOwnTradeReceived(self, trade):
        super(bot_for_spot_market_custom_grid_strategy, self).OnOwnTradeReceived(trade)

        if trade.Order is None or trade.Trade is None:
            return

        price = trade.Trade.TradePrice
        volume = trade.Trade.TradeVolume
        if price is None or volume is None:
            return

        if trade.Order.Side == Sides.Buy:
            new_volume = self._bought_volume + volume
            if new_volume > 0:
                self._avg_price = (self._avg_price * self._bought_volume + price * volume) / new_volume
            self._bought_volume = new_volume
            self._last_entry_price = price
        elif self.Position <= 0:
            self._reset_grid()

    def CreateClone(self):
        return bot_for_spot_market_custom_grid_strategy()
