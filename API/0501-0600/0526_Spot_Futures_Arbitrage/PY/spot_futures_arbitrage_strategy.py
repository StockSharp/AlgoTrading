import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, InvalidOperationException
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes, OrderStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Order, Security
from indicator_extensions import *

class spot_futures_arbitrage_strategy(Strategy):
    def __init__(self):
        super(spot_futures_arbitrage_strategy, self).__init__()

        self._spot = self.Param[Security]("Spot", None) \
            .SetDisplay("Spot", "Spot security", "General")
        self._future = self.Param[Security]("Future", None) \
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

        self._spot_order = None
        self._future_order = None
        self._spot_price = 0.0
        self._future_price = 0.0
        self._entry_volume = 0.0
        self._is_long = False
        self._in_position = False
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

    def GetWorkingSecurities(self):
        if self.spot is None or self.future is None:
            raise InvalidOperationException("Both spot and futures securities must be set.")
        return [(self.spot, self.candle_type), (self.future, self.candle_type)]

    def OnReseted(self):
        super(spot_futures_arbitrage_strategy, self).OnReseted()
        self._spot_order = None
        self._future_order = None
        self._spot_price = 0.0
        self._future_price = 0.0
        self._entry_volume = 0.0
        self._is_long = False
        self._in_position = False
        self._entry_time = None

    def OnStarted2(self, time):
        if self.spot is None or self.future is None:
            raise InvalidOperationException("Both spot and futures securities must be set.")

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

        # The pair is opened and closed as a whole, so a new decision may not be taken while
        # an earlier market order of either leg is still working.
        if self._is_working(self._spot_order) or self._is_working(self._future_order):
            return

        if not self._in_position:
            volume = self.Volume

            if spread >= entry_long:
                self._spot_order = self._register(self.spot, Sides.Buy, volume)
                self._future_order = self._register(self.future, Sides.Sell, volume)
                self._is_long = True
                self._in_position = True
                self._entry_volume = volume
                self._entry_time = now
            elif spread <= entry_short:
                self._spot_order = self._register(self.spot, Sides.Sell, volume)
                self._future_order = self._register(self.future, Sides.Buy, volume)
                self._is_long = False
                self._in_position = True
                self._entry_volume = volume
                self._entry_time = now
        else:
            time_expired = (now - self._entry_time) >= TimeSpan.FromHours(int(self.max_hold_hours))
            if self._is_long:
                should_exit = spread < entry_long * exit_threshold
            else:
                should_exit = spread > entry_short * exit_threshold

            if should_exit or time_expired:
                # Each leg is closed with exactly the volume it was opened with, so the
                # order size never depends on the results of the previous cycle.
                spot_side = Sides.Sell if self._is_long else Sides.Buy
                future_side = Sides.Buy if self._is_long else Sides.Sell
                self._spot_order = self._register(self.spot, spot_side, self._entry_volume)
                self._future_order = self._register(self.future, future_side, self._entry_volume)

                self._is_long = False
                self._in_position = False
                self._entry_volume = 0.0
                self._entry_time = None

    def _is_working(self, order):
        return order is not None and order.State != OrderStates.Done and order.State != OrderStates.Failed

    def _register(self, security, side, volume):
        order = Order()
        order.Security = security
        order.Portfolio = self.Portfolio
        order.Side = side
        order.Volume = volume
        order.Type = OrderTypes.Market
        self.RegisterOrder(order)
        return order

    def CreateClone(self):
        return spot_futures_arbitrage_strategy()
