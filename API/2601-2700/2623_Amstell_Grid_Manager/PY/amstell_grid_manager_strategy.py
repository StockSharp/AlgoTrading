import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class amstell_grid_manager_strategy(Strategy):
    """Amstell averaging grid strategy with TP/SL and distance-based grid entries."""

    def __init__(self):
        super(amstell_grid_manager_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Quantity submitted with each grid order", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._buy_distance_pips = self.Param("BuyDistancePips", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Buy Distance (pips)", "Distance before adding another long", "Entries")
        self._sell_distance_mult = self.Param("SellDistanceMultiplier", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Sell Distance Multiplier", "Multiplier applied to long distance for shorts", "Entries")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for processing", "General")

        self._long_volume = 0.0
        self._short_volume = 0.0
        self._avg_long_price = None
        self._avg_short_price = None
        self._last_buy_price = None
        self._last_sell_price = None
        self._pip_value = 0.0
        self._tp_offset = 0.0
        self._sl_offset = 0.0
        self._buy_dist_offset = 0.0
        self._sell_dist_offset = 0.0
        self._closing_long = False
        self._closing_short = False

    @property
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def BuyDistancePips(self):
        return self._buy_distance_pips.Value
    @property
    def SellDistanceMultiplier(self):
        return self._sell_distance_mult.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_value(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(amstell_grid_manager_strategy, self).OnStarted2(time)

        self.Volume = self.OrderVolume
        self._pip_value = self._calc_pip_value()
        self._tp_offset = self.TakeProfitPips * self._pip_value
        self._sl_offset = self.StopLossPips * self._pip_value
        self._buy_dist_offset = self.BuyDistancePips * self._pip_value
        self._sell_dist_offset = self._buy_dist_offset * float(self.SellDistanceMultiplier)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Check long TP/SL
        if not self._closing_long and self._long_volume > 0 and self._avg_long_price is not None:
            profit = close - self._avg_long_price
            if profit >= self._tp_offset or -profit >= self._sl_offset:
                self.SellMarket()
                self._closing_long = True
                return

        # Check short TP/SL
        if not self._closing_short and self._short_volume > 0 and self._avg_short_price is not None:
            profit = self._avg_short_price - close
            if profit >= self._tp_offset or -profit >= self._sl_offset:
                self.BuyMarket()
                self._closing_short = True
                return

        opened_long = False

        # Grid long entries
        if not self._closing_long and self.Position >= 0:
            if self._long_volume <= 0:
                self.BuyMarket()
                self._record_buy(close)
                opened_long = True
            elif self._last_buy_price is not None and self._last_buy_price - close >= self._buy_dist_offset:
                self.BuyMarket()
                self._record_buy(close)
                opened_long = True

        if opened_long:
            return

        # Grid short entries
        if not self._closing_short and self.Position <= 0:
            if self._short_volume <= 0:
                self.SellMarket()
                self._record_sell(close)
            elif self._last_sell_price is not None and close - self._last_sell_price >= self._sell_dist_offset:
                self.SellMarket()
                self._record_sell(close)

    def _record_buy(self, price):
        vol = float(self.Volume) if self.Volume > 0 else 1.0
        new_vol = self._long_volume + vol
        total_cost = (self._avg_long_price if self._avg_long_price is not None else 0.0) * self._long_volume + price * vol
        self._long_volume = new_vol
        self._avg_long_price = total_cost / new_vol if new_vol > 0 else price
        self._last_buy_price = price
        self._closing_long = False

    def _record_sell(self, price):
        vol = float(self.Volume) if self.Volume > 0 else 1.0
        new_vol = self._short_volume + vol
        total_cost = (self._avg_short_price if self._avg_short_price is not None else 0.0) * self._short_volume + price * vol
        self._short_volume = new_vol
        self._avg_short_price = total_cost / new_vol if new_vol > 0 else price
        self._last_sell_price = price
        self._closing_short = False

    def _check_position_sync(self):
        if self.Position == 0:
            self._long_volume = 0.0
            self._short_volume = 0.0
            self._avg_long_price = None
            self._avg_short_price = None
            self._last_buy_price = None
            self._last_sell_price = None
            self._closing_long = False
            self._closing_short = False

    def OnReseted(self):
        super(amstell_grid_manager_strategy, self).OnReseted()
        self._long_volume = 0.0
        self._short_volume = 0.0
        self._avg_long_price = None
        self._avg_short_price = None
        self._last_buy_price = None
        self._last_sell_price = None
        self._pip_value = 0.0
        self._tp_offset = 0.0
        self._sl_offset = 0.0
        self._buy_dist_offset = 0.0
        self._sell_dist_offset = 0.0
        self._closing_long = False
        self._closing_short = False

    def CreateClone(self):
        return amstell_grid_manager_strategy()
