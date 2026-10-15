import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Sides
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class timer_strategy(Strategy):
    def __init__(self):
        super(timer_strategy, self).__init__()
        self._wait_seconds = self.Param("WaitSeconds", 60).SetGreaterThanZero()
        self._pip_distance = self.Param("PipDistance", 10.0).SetNotNegative()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero()
        self._take_profit = self.Param("TakeProfit", 100.0).SetNotNegative()
        self._stop_loss = self.Param("StopLoss", 50.0).SetNotNegative()
        self._trailing_stop = self.Param("TrailingStop", 0.0).SetNotNegative()
        self._trade_volume = self.Param("TradeVolume", 1.0).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._use_trading_hours = self.Param("UseTradingHours", False)
        self._start_time = self.Param("StartTime", TimeSpan.Zero)
        self._stop_time = self.Param("StopTime", TimeSpan(23, 59, 59))

        self._pending_orders = []
        self._last_level_time = None
        self._buy_level = None
        self._sell_level = None
        self._trail_extreme = None

        self.OrderRegistering += self._track_pending_order

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(timer_strategy, self).OnReseted()
        self._last_level_time = None
        self._buy_level = None
        self._sell_level = None
        self._trail_extreme = None
        self._pending_orders = []

    def OnStarted2(self, time):
        super(timer_strategy, self).OnStarted2(time)

        point = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if point <= 0:
            point = 1.0

        tp = float(self._take_profit.Value)
        sl = float(self._stop_loss.Value)

        take = Unit(tp * point, UnitTypes.Absolute) if tp > 0 else None
        stop = Unit(sl * point, UnitTypes.Absolute) if sl > 0 else None

        if take is not None or stop is not None:
            self.StartProtection(take, stop, useMarketOrders=True)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        def on_candle(candle, atr_value):
            if candle.State != CandleStates.Finished or not atr.IsFormed or float(atr_value) <= 0:
                return
            self._process_candle(candle, float(atr_value))

        self.SubscribeCandles(self._candle_type.Value).Bind(atr, on_candle).Start()

    def _process_candle(self, candle, atr):
        now = candle.CloseTime

        step = self.Security.PriceStep if self.Security is not None else None
        point = step if step is not None and step > 0 else Decimal.One

        position = self._apply_trailing_stop(candle, self._expected_position(), point)

        if self._is_trading_time(now.TimeOfDay):
            close = float(candle.ClosePrice)
            volume = float(self._trade_volume.Value) + abs(float(position))

            if self._buy_level is not None and close >= self._buy_level and position <= 0:
                self.BuyMarket(volume)
                self._trail_extreme = candle.ClosePrice
            elif self._sell_level is not None and close <= self._sell_level and position >= 0:
                self.SellMarket(volume)
                self._trail_extreme = candle.ClosePrice

        wait = int(self._wait_seconds.Value)
        if self._last_level_time is None or (now - self._last_level_time).TotalSeconds >= wait:
            self._buy_level, self._sell_level = self.calculate_levels(
                float(candle.ClosePrice), float(self._pip_distance.Value), float(point), atr)
            self._last_level_time = now

    def _track_pending_order(self, order):
        self._pending_orders.append(order)

    def _expected_position(self):
        # Protection registers its exit before the candle handler runs, so the handler must not
        # close or size against the position that exit closes.
        self._pending_orders = [o for o in self._pending_orders if o.State not in (OrderStates.Done, OrderStates.Failed)]
        position = self.Position
        for order in self._pending_orders:
            if order.Side == Sides.Buy:
                position += order.Balance
            else:
                position -= order.Balance
        return position

    def _apply_trailing_stop(self, candle, position, point):
        if position == 0:
            self._trail_extreme = None
            return position

        trailing_stop = float(self._trailing_stop.Value)
        if trailing_stop <= 0 or self._trail_extreme is None:
            return position

        trail = Decimal(trailing_stop) * point
        extreme = self._trail_extreme

        # Earlier candles set the extreme: within this candle the order of high and low is unknown.
        if position > 0:
            if candle.LowPrice <= extreme - trail:
                self.SellMarket(position)
                self._trail_extreme = None
                return Decimal.Zero
            self._trail_extreme = Math.Max(extreme, candle.HighPrice)
        else:
            if candle.HighPrice >= extreme + trail:
                self.BuyMarket(Math.Abs(position))
                self._trail_extreme = None
                return Decimal.Zero
            self._trail_extreme = Math.Min(extreme, candle.LowPrice)

        return position

    def _is_trading_time(self, value):
        if not bool(self._use_trading_hours.Value):
            return True
        start = self._start_time.Value
        stop = self._stop_time.Value
        return start <= value <= stop if start <= stop else value >= start or value <= stop

    @staticmethod
    def calculate_levels(close, pip_distance_points, price_step, atr):
        distance = pip_distance_points * price_step + atr
        return close + distance, close - distance

    def CreateClone(self):
        return timer_strategy()
