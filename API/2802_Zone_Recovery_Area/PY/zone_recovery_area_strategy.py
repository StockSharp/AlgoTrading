import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType
from System import TimeSpan, Math


class zone_recovery_area_strategy(Strategy):
    def __init__(self):
        super(zone_recovery_area_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._fast_ma_length = self.Param("FastMaLength", 20)
        self._slow_ma_length = self.Param("SlowMaLength", 200)
        self._take_profit_pips = self.Param("TakeProfitPips", 150.0)
        self._zone_recovery_pips = self.Param("ZoneRecoveryPips", 50.0)
        self._initial_volume = self.Param("InitialVolume", 1.0)
        self._use_volume_multiplier = self.Param("UseVolumeMultiplier", True)
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0)
        self._volume_increment = self.Param("VolumeIncrement", 0.5)
        self._max_trades = self.Param("MaxTrades", 6)
        self._enable_trailing = self.Param("EnableTrailing", True)
        self._trailing_start_profit = self.Param("TrailingStartProfit", 40.0)
        self._trailing_drawdown = self.Param("TrailingDrawdown", 10.0)

        self._fast_ma = None
        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._ma_initialized = False
        self._is_long_cycle = False
        self._cycle_base_price = 0.0
        self._next_step_index = 0
        self._peak_cycle_profit = 0.0
        self._steps = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    def OnStarted(self, time):
        super(zone_recovery_area_strategy, self).OnStarted(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self._fast_ma_length.Value
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self._slow_ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ma, self._slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        fast_value = float(fast_val)
        slow_value = float(slow_val)

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            return

        if not self._ma_initialized:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._ma_initialized = True
            return

        price = float(candle.ClosePrice)

        if len(self._steps) > 0:
            self._handle_existing_cycle(price)
        else:
            self._try_start_cycle(price)

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def _try_start_cycle(self, price):
        bullish = self._prev_fast < self._prev_slow
        bearish = self._prev_fast > self._prev_slow
        if bullish:
            self._start_cycle(True, price)
        elif bearish:
            self._start_cycle(False, price)

    def _start_cycle(self, is_long, price):
        vol = self.InitialVolume
        if vol <= 0:
            return
        self._steps = []
        self._is_long_cycle = is_long
        self._cycle_base_price = price
        self._next_step_index = 1
        self._peak_cycle_profit = 0.0
        self._execute_order(is_long, vol, price)

    def _handle_existing_cycle(self, price):
        tp_offset = self._get_price_offset(self._take_profit_pips.Value)
        if tp_offset > 0:
            if self._is_long_cycle and price >= self._cycle_base_price + tp_offset:
                self._close_cycle()
                return
            if not self._is_long_cycle and price <= self._cycle_base_price - tp_offset:
                self._close_cycle()
                return

        cycle_profit = self._calculate_cycle_profit(price)

        if self._enable_trailing.Value and self._trailing_start_profit.Value > 0 and self._trailing_drawdown.Value > 0:
            if cycle_profit >= self._trailing_start_profit.Value:
                self._peak_cycle_profit = max(self._peak_cycle_profit, cycle_profit)
            if self._peak_cycle_profit > 0 and cycle_profit <= self._peak_cycle_profit - self._trailing_drawdown.Value:
                self._close_cycle()
                return

        if len(self._steps) >= self._max_trades.Value:
            return
        if not self._should_open_next(price):
            return

        next_buy = self._get_next_direction()
        volume = self._get_next_volume()
        self._execute_order(next_buy, volume, price)
        self._next_step_index += 1

    def _should_open_next(self, price):
        zone_offset = self._get_price_offset(self._zone_recovery_pips.Value)
        if zone_offset <= 0:
            return False
        next_buy = self._get_next_direction()
        if self._is_long_cycle:
            return price >= self._cycle_base_price if next_buy else price <= self._cycle_base_price - zone_offset
        else:
            return price >= self._cycle_base_price + zone_offset if next_buy else price <= self._cycle_base_price

    def _get_next_direction(self):
        is_odd = self._next_step_index % 2 == 1
        return not is_odd if self._is_long_cycle else is_odd

    def _get_next_volume(self):
        if len(self._steps) == 0:
            return self.InitialVolume
        last_vol = self._steps[-1][2]
        if self._use_volume_multiplier.Value:
            nv = last_vol * self._volume_multiplier.Value
        else:
            nv = last_vol + self._volume_increment.Value
        return nv if nv > 0 else self.InitialVolume

    def _calculate_cycle_profit(self, price):
        if len(self._steps) == 0 or self.Security is None:
            return 0.0
        ps = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 0.0
        if ps <= 0:
            return 0.0
        pnl = 0.0
        for is_buy, step_price, vol in self._steps:
            diff = price - step_price
            steps_count = diff / ps
            direction = 1.0 if is_buy else -1.0
            pnl += steps_count * ps * vol * direction
        return pnl

    def _get_price_offset(self, pips):
        if self.Security is None:
            return 0.0
        ps = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 0.0
        return pips * ps if ps > 0 else 0.0

    def _execute_order(self, is_buy, volume, price):
        if volume <= 0:
            return
        if is_buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)
        self._steps.append((is_buy, price, volume))

    def _close_cycle(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self._steps = []
        self._next_step_index = 0
        self._cycle_base_price = 0.0
        self._peak_cycle_profit = 0.0

    def OnReseted(self):
        super(zone_recovery_area_strategy, self).OnReseted()
        self._steps = []
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._ma_initialized = False
        self._is_long_cycle = False
        self._cycle_base_price = 0.0
        self._next_step_index = 0
        self._peak_cycle_profit = 0.0

    def CreateClone(self):
        return zone_recovery_area_strategy()
