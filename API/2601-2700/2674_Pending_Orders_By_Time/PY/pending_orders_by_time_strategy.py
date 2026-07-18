import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pending_orders_by_time_strategy(Strategy):
    """Places symmetric stop entries at scheduled hours with daily resets and SL/TP management."""

    def __init__(self):
        super(pending_orders_by_time_strategy, self).__init__()

        self._opening_hour = self.Param("OpeningHour", 2) \
            .SetDisplay("Opening Hour", "Hour to activate pending orders", "Schedule")
        self._closing_hour = self.Param("ClosingHour", 22) \
            .SetDisplay("Closing Hour", "Hour to cancel orders and flat positions", "Schedule")
        self._distance_pips = self.Param("DistancePips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Distance (pips)", "Offset for entry stop orders", "Orders")
        self._stop_loss_pips = self.Param("StopLossPips", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 2000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Profit target distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Working timeframe for the schedule", "General")

        self._pip_size = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = None

    @property
    def OpeningHour(self):
        return int(self._opening_hour.Value)
    @property
    def ClosingHour(self):
        return int(self._closing_hour.Value)
    @property
    def DistancePips(self):
        return float(self._distance_pips.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calculate_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        if step <= 0:
            return 0.01
        return step

    def OnStarted2(self, time):
        super(pending_orders_by_time_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour

        # Check pending stop entries
        self._check_pending_entries(candle)

        # Manage existing position
        self._manage_risk(candle)

        if hour == self.ClosingHour:
            # Closing hour: cancel pending and exit any open trades
            self._pending_buy_price = None
            self._pending_sell_price = None
            self._exit_position()

        if hour == self.OpeningHour and hour != self.ClosingHour and self.Position == 0 and self._pending_buy_price is None:
            # Opening hour: set up new pending entries
            self._setup_pending_entries(float(candle.ClosePrice))

    def _check_pending_entries(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._pending_buy_price is not None and h >= self._pending_buy_price and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = self._pending_buy_price
            self._pending_buy_price = None
            self._pending_sell_price = None
            return

        if self._pending_sell_price is not None and lo <= self._pending_sell_price and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = self._pending_sell_price
            self._pending_buy_price = None
            self._pending_sell_price = None

    def _manage_risk(self, candle):
        if self._pip_size <= 0 or self._entry_price is None:
            return

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        entry = self._entry_price

        tp_dist = self.TakeProfitPips * self._pip_size
        sl_dist = self.StopLossPips * self._pip_size

        if self.Position > 0:
            if tp_dist > 0 and h - entry >= tp_dist:
                self.SellMarket()
                self._entry_price = None
                return
            if sl_dist > 0 and entry - lo >= sl_dist:
                self.SellMarket()
                self._entry_price = None

        elif self.Position < 0:
            if tp_dist > 0 and entry - lo >= tp_dist:
                self.BuyMarket()
                self._entry_price = None
                return
            if sl_dist > 0 and h - entry >= sl_dist:
                self.BuyMarket()
                self._entry_price = None

    def _exit_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._entry_price = None

    def _setup_pending_entries(self, reference_price):
        if self._pip_size <= 0:
            return

        distance = self.DistancePips * self._pip_size
        if distance <= 0:
            return

        self._pending_buy_price = reference_price + distance
        self._pending_sell_price = reference_price - distance

    def OnReseted(self):
        super(pending_orders_by_time_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._pending_buy_price = None
        self._pending_sell_price = None
        self._entry_price = None

    def CreateClone(self):
        return pending_orders_by_time_strategy()
