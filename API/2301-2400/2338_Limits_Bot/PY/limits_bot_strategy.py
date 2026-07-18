import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class limits_bot_strategy(Strategy):
    def __init__(self):
        super(limits_bot_strategy, self).__init__()
        self._buy_allow = self.Param("BuyAllow", True) \
            .SetDisplay("Buy Allow", "Enable long orders", "Trading")
        self._sell_allow = self.Param("SellAllow", True) \
            .SetDisplay("Sell Allow", "Enable short orders", "Trading")
        self._stop_order_distance = self.Param("StopOrderDistance", 5.0) \
            .SetDisplay("Stop Order Distance", "Distance from open price", "Risk")
        self._take_profit = self.Param("TakeProfit", 35.0) \
            .SetDisplay("Take Profit", "Take profit in ticks", "Risk")
        self._stop_loss = self.Param("StopLoss", 8.0) \
            .SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")
        self._trailing_start = self.Param("TrailingStart", 40.0) \
            .SetDisplay("Trailing Start", "Profit to activate trailing", "Risk")
        self._trailing_distance = self.Param("TrailingDistance", 30.0) \
            .SetDisplay("Trailing Distance", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 1.0) \
            .SetDisplay("Trailing Step", "Minimal move to shift trailing", "Risk")
        self._cooldown_candles = self.Param("CooldownCandles", 2) \
            .SetDisplay("Cooldown Candles", "Bars to wait after an exit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._buy_order = None
        self._sell_order = None
        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._last_position = 0.0
        self._bars_since_exit = 0

    @property
    def buy_allow(self):
        return self._buy_allow.Value

    @property
    def sell_allow(self):
        return self._sell_allow.Value

    @property
    def stop_order_distance(self):
        return self._stop_order_distance.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def trailing_start(self):
        return self._trailing_start.Value

    @property
    def trailing_distance(self):
        return self._trailing_distance.Value

    @property
    def trailing_step(self):
        return self._trailing_step.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(limits_bot_strategy, self).OnReseted()
        self._buy_order = None
        self._sell_order = None
        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._last_position = 0.0
        self._bars_since_exit = int(self.cooldown_candles)

    def OnStarted2(self, time):
        super(limits_bot_strategy, self).OnStarted2(time)
        self._buy_order = None
        self._sell_order = None
        self._entry_price = None
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._last_position = 0.0
        self._bars_since_exit = int(self.cooldown_candles)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price_step = 0.01
        self._bars_since_exit += 1
        pos = float(self.Position)
        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        sl = float(self.stop_loss)
        tp = float(self.take_profit)
        ts = float(self.trailing_start)
        td = float(self.trailing_distance)
        tstep = float(self.trailing_step)
        cd = int(self.cooldown_candles)

        if pos > 0 and self._last_position <= 0:
            self._entry_price = open_price
            self._long_stop = self._entry_price - sl * price_step
            self._long_take = self._entry_price + tp * price_step
            if self._sell_order is not None:
                self.CancelOrder(self._sell_order)
                self._sell_order = None
        elif pos < 0 and self._last_position >= 0:
            self._entry_price = open_price
            self._short_stop = self._entry_price + sl * price_step
            self._short_take = self._entry_price - tp * price_step
            if self._buy_order is not None:
                self.CancelOrder(self._buy_order)
                self._buy_order = None

        if pos > 0 and self._entry_price is not None:
            entry_long = self._entry_price
            if td > 0 and ts > 0 and close_price - entry_long >= ts * price_step:
                new_stop = close_price - td * price_step
                if self._long_stop is None or new_stop >= self._long_stop + tstep * price_step:
                    self._long_stop = new_stop
            if (self._long_stop is not None and low_price <= self._long_stop) or \
               (self._long_take is not None and high_price >= self._long_take):
                self.SellMarket()
                self._entry_price = None
                self._long_stop = None
                self._long_take = None
                self._bars_since_exit = 0
        elif pos < 0 and self._entry_price is not None:
            entry_short = self._entry_price
            if td > 0 and ts > 0 and entry_short - close_price >= ts * price_step:
                new_stop = close_price + td * price_step
                if self._short_stop is None or new_stop <= self._short_stop - tstep * price_step:
                    self._short_stop = new_stop
            if (self._short_stop is not None and high_price >= self._short_stop) or \
               (self._short_take is not None and low_price <= self._short_take):
                self.BuyMarket()
                self._entry_price = None
                self._short_stop = None
                self._short_take = None
                self._bars_since_exit = 0
        elif pos == 0 and self._bars_since_exit >= cd:
            self._entry_price = None
            self._long_stop = None
            self._long_take = None
            self._short_stop = None
            self._short_take = None
            if self._buy_order is not None:
                self.CancelOrder(self._buy_order)
                self._buy_order = None
            if self._sell_order is not None:
                self.CancelOrder(self._sell_order)
                self._sell_order = None
            sod = float(self.stop_order_distance)
            if self.buy_allow and close_price >= open_price:
                self._buy_order = self.BuyLimit(open_price - sod * price_step, self.Volume)
            if self.sell_allow and close_price <= open_price:
                self._sell_order = self.SellLimit(open_price + sod * price_step, self.Volume)

        self._last_position = pos

    def CreateClone(self):
        return limits_bot_strategy()
