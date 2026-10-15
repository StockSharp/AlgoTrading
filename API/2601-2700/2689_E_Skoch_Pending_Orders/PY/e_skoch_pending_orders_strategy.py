import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class e_skoch_pending_orders_strategy(Strategy):
    """Pending breakout: detects falling highs or rising lows to enter on breakouts with SL/TP."""

    def __init__(self):
        super(e_skoch_pending_orders_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._take_profit_buy_pips = self.Param("TakeProfitBuyPips", 2000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy TP (pips)", "Long take profit distance", "Trading")
        self._stop_loss_buy_pips = self.Param("StopLossBuyPips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy SL (pips)", "Long stop loss distance", "Trading")
        self._take_profit_sell_pips = self.Param("TakeProfitSellPips", 2000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell TP (pips)", "Short take profit distance", "Trading")
        self._stop_loss_sell_pips = self.Param("StopLossSellPips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell SL (pips)", "Short stop loss distance", "Trading")
        self._indent_high_pips = self.Param("IndentHighPips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("High Indent", "Buy stop offset", "Trading")
        self._indent_low_pips = self.Param("IndentLowPips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Low Indent", "Sell stop offset", "Trading")
        self._check_existing_trade = self.Param("CheckExistingTrade", True) \
            .SetDisplay("Block During Position", "Skip signals when a position exists", "Risk")

        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._pip_value = 1.0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def TakeProfitBuyPips(self):
        return float(self._take_profit_buy_pips.Value)
    @property
    def StopLossBuyPips(self):
        return float(self._stop_loss_buy_pips.Value)
    @property
    def TakeProfitSellPips(self):
        return float(self._take_profit_sell_pips.Value)
    @property
    def StopLossSellPips(self):
        return float(self._stop_loss_sell_pips.Value)
    @property
    def IndentHighPips(self):
        return float(self._indent_high_pips.Value)
    @property
    def IndentLowPips(self):
        return float(self._indent_low_pips.Value)
    @property
    def CheckExistingTrade(self):
        return self._check_existing_trade.Value

    def OnStarted2(self, time):
        super(e_skoch_pending_orders_strategy, self).OnStarted2(time)

        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        self._pip_value = price_step if price_step > 0 else 1.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        # Check pending entries
        self._check_pending_entries(h, lo)

        # Manage SL/TP
        self._manage_position(h, lo)

        # Need at least 2 previous bars
        if self._prev_high1 is None:
            self._prev_high1 = h
            self._prev_low1 = lo
            return

        if self._prev_high2 is None:
            self._prev_high2 = self._prev_high1
            self._prev_low2 = self._prev_low1
            self._prev_high1 = h
            self._prev_low1 = lo
            return

        has_position = self.Position != 0

        # Falling highs -> place buy stop above recent high
        if self._prev_high2 > self._prev_high1 and not has_position:
            if not self.CheckExistingTrade or self.Position == 0:
                buy_price = self._prev_high1 + self._pip_value * self.IndentHighPips
                self._pending_buy_price = buy_price
                self._long_stop = buy_price - self._pip_value * self.StopLossBuyPips
                self._long_take = buy_price + self._pip_value * self.TakeProfitBuyPips

        # Rising lows -> place sell stop below recent low
        if self._prev_low2 < self._prev_low1 and not has_position:
            if not self.CheckExistingTrade or self.Position == 0:
                sell_price = self._prev_low1 - self._pip_value * self.IndentLowPips
                self._pending_sell_price = sell_price
                self._short_stop = sell_price + self._pip_value * self.StopLossSellPips
                self._short_take = sell_price - self._pip_value * self.TakeProfitSellPips

        # Shift history
        self._prev_high2 = self._prev_high1
        self._prev_low2 = self._prev_low1
        self._prev_high1 = h
        self._prev_low1 = lo

    def _check_pending_entries(self, h, lo):
        if self.Position != 0:
            return

        if self._pending_buy_price is not None and h >= self._pending_buy_price:
            self.BuyMarket()
            self._entry_price = self._pending_buy_price
            self._pending_buy_price = None
            self._pending_sell_price = None
            return

        if self._pending_sell_price is not None and lo <= self._pending_sell_price:
            self.SellMarket()
            self._entry_price = self._pending_sell_price
            self._pending_buy_price = None
            self._pending_sell_price = None

    def _manage_position(self, h, lo):
        if self.Position > 0:
            if self._long_stop > 0 and lo <= self._long_stop:
                self.SellMarket()
                self._reset_position_state()
                return
            if self._long_take > 0 and h >= self._long_take:
                self.SellMarket()
                self._reset_position_state()
        elif self.Position < 0:
            if self._short_stop > 0 and h >= self._short_stop:
                self.BuyMarket()
                self._reset_position_state()
                return
            if self._short_take > 0 and lo <= self._short_take:
                self.BuyMarket()
                self._reset_position_state()

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None

    def OnReseted(self):
        super(e_skoch_pending_orders_strategy, self).OnReseted()
        self._prev_high1 = None
        self._prev_high2 = None
        self._prev_low1 = None
        self._prev_low2 = None
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = 0.0
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0
        self._pip_value = 1.0

    def CreateClone(self):
        return e_skoch_pending_orders_strategy()
