import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order
from indicator_extensions import *

class spot_futures_arbitrage_strategy(Strategy):
    def __init__(self):
        super(spot_futures_arbitrage_strategy, self).__init__()

        self._spot = self.Param("Spot", None) \
            .SetDisplay("Spot", "Spot security", "General")
        self._future = self.Param("Future", None) \
            .SetDisplay("Future", "Futures security", "General")
        self._min_spread_pct = self.Param("MinSpreadPct", 0.05) \
            .SetDisplay("Min Spread %", "Minimum spread percentage to enter", "General")
        self._lookback = self.Param("LookbackPeriod", 5) \
            .SetDisplay("Lookback", "Period for spread statistics", "General")
        self._adaptive = self.Param("AdaptiveThreshold", True) \
            .SetDisplay("Adaptive Threshold", "Use dynamic thresholds", "General")
        self._max_hold_hours = self.Param("MaxHoldHours", 6) \
            .SetDisplay("Max Hold Hours", "Maximum holding time in hours", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._spot_price = 0.0
        self._future_price = 0.0
        self._is_long = False
        self._entry_time = None

    @property
    def spot(self):
        return self._spot.Value

    @property
    def future(self):
        return self._future.Value

    @property
    def min_spread_pct(self):
        return self._min_spread_pct.Value

    @property
    def lookback_period(self):
        return self._lookback.Value

    @property
    def adaptive_threshold(self):
        return self._adaptive.Value

    @property
    def max_hold_hours(self):
        return self._max_hold_hours.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spot_futures_arbitrage_strategy, self).OnReseted()
        self._spot_price = 0.0
        self._future_price = 0.0
        self._is_long = False
        self._entry_time = None

    def OnStarted2(self, time):
        super(spot_futures_arbitrage_strategy, self).OnStarted2(time)

        self._spread_average = SMA()
        self._spread_average.Length = self.lookback_period
        self._spread_std = StandardDeviation()
        self._spread_std.Length = self.lookback_period

        spot_sub = self.SubscribeCandles(self.candle_type, True, self.spot)
        spot_sub.Bind(lambda c: self._process_candle(c, True)).Start()

        self.SubscribeCandles(self.candle_type, True, self.future) \
            .Bind(lambda c: self._process_candle(c, False)).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, spot_sub)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, is_spot):
        if candle.State != CandleStates.Finished:
            return

        if is_spot:
            self._spot_price = float(candle.ClosePrice)
        else:
            self._future_price = float(candle.ClosePrice)

        if self._spot_price <= 0 or self._future_price <= 0:
            return

        spread = (self._future_price - self._spot_price) / self._spot_price

        avg_val = process_float(self._spread_average, spread, candle.ServerTime, True)
        avg = float(avg_val)
        std_val = process_float(self._spread_std, spread, candle.ServerTime, True)
        std = float(std_val)

        min_spread = float(self.min_spread_pct) / 100.0
        entry_long = min_spread
        entry_short = -min_spread

        if self.adaptive_threshold and self._spread_average.IsFormed and self._spread_std.IsFormed:
            entry_long = max(min_spread, avg + std * 1.5)
            entry_short = min(-min_spread, avg - std * 1.5)

        exit_threshold = 0.6
        now = candle.CloseTime

        spot_pos = float(self.GetPositionValue(self.spot, self.Portfolio) or 0)
        fut_pos = float(self.GetPositionValue(self.future, self.Portfolio) or 0)
        has_position = spot_pos != 0 or fut_pos != 0

        if not has_position:
            if spread >= entry_long:
                self._buy_security(self.spot)
                self._sell_security(self.future)
                self._is_long = True
                self._entry_time = now
            elif spread <= entry_short:
                self._sell_security(self.spot)
                self._buy_security(self.future)
                self._is_long = False
                self._entry_time = now
        else:
            time_expired = self._entry_time is not None and (now - self._entry_time) >= TimeSpan.FromHours(int(self.max_hold_hours))
            if self._is_long:
                should_exit = spread < entry_long * exit_threshold
            else:
                should_exit = spread > entry_short * exit_threshold

            if should_exit or time_expired:
                if spot_pos != 0:
                    order = Order()
                    order.Security = self.spot
                    order.Portfolio = self.Portfolio
                    order.Side = Sides.Sell if spot_pos > 0 else Sides.Buy
                    order.Volume = abs(spot_pos)
                    order.Type = OrderTypes.Market
                    self.RegisterOrder(order)

                if fut_pos != 0:
                    order = Order()
                    order.Security = self.future
                    order.Portfolio = self.Portfolio
                    order.Side = Sides.Sell if fut_pos > 0 else Sides.Buy
                    order.Volume = abs(fut_pos)
                    order.Type = OrderTypes.Market
                    self.RegisterOrder(order)

                self._is_long = False
                self._entry_time = None

    def _buy_security(self, security):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Buy
        order.Volume = self.Volume
        order.Type = OrderTypes.Market
        self.RegisterOrder(order)

    def _sell_security(self, security):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = Sides.Sell
        order.Volume = self.Volume
        order.Type = OrderTypes.Market
        self.RegisterOrder(order)

    def CreateClone(self):
        return spot_futures_arbitrage_strategy()
