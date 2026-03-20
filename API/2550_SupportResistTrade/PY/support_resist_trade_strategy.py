import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

TREND_NONE = 0
TREND_BULLISH = 1
TREND_BEARISH = 2


class support_resist_trade_strategy(Strategy):
    def __init__(self):
        super(support_resist_trade_strategy, self).__init__()

        self._lookback = self.Param("Lookback", 55)
        self._ma_period = self.Param("MaPeriod", 500)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._order_volume = self.Param("OrderVolume", 0.1)

        self._prev_support = None
        self._prev_resistance = None
        self._long_stop = None
        self._short_stop = None
        self._trend = TREND_NONE
        self._pip_size = 0.0
        self._levels_initialized = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    def OnStarted(self, time):
        super(support_resist_trade_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod
        self._highest = Highest()
        self._highest.Length = self.Lookback
        self._lowest = Lowest()
        self._lowest.Length = self.Lookback
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self.ProcessCandle).Start()

        ps = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        self._pip_size = ps
        if self.Security is not None and self.Security.Decimals is not None:
            d = self.Security.Decimals
            if d == 3 or d == 5:
                self._pip_size = ps * 10.0

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        high_result = self._highest.Process(self._highest.CreateValue(candle.OpenTime, float(candle.HighPrice)))
        low_result = self._lowest.Process(self._lowest.CreateValue(candle.OpenTime, float(candle.LowPrice)))

        if not self._ema.IsFormed or not high_result.IsFormed or not low_result.IsFormed:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        support = float(low_result)
        resistance = float(high_result)
        ema = float(ema_value)
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if not self._levels_initialized:
            self._prev_support = support
            self._prev_resistance = resistance
            self._levels_initialized = True
            return

        if open_price > ema:
            self._trend = TREND_BULLISH
        elif open_price < ema:
            self._trend = TREND_BEARISH

        exit_placed = self._manage_position(candle)

        if not exit_placed and self._cooldown_remaining == 0 and self.Position == 0:
            if self._trend == TREND_BULLISH and self._prev_resistance is not None and close > self._prev_resistance:
                self.BuyMarket()
                self._entry_price = close
                self._long_stop = self._prev_support
                self._short_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)
            elif self._trend == TREND_BEARISH and self._prev_support is not None and close < self._prev_support:
                self.SellMarket()
                self._entry_price = close
                self._short_stop = self._prev_resistance
                self._long_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)

        self._prev_support = support
        self._prev_resistance = resistance

    def _manage_position(self, candle):
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self._long_stop is not None and close <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)
                return True

            entry = self._entry_price
            profit_per_unit = close - entry

            if profit_per_unit > 0.0 and self._prev_support is not None and close < self._prev_support:
                self.SellMarket()
                self._long_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)
                return True

            self._update_long_trailing(close, entry)

        elif self.Position < 0:
            if self._short_stop is not None and close >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)
                return True

            entry = self._entry_price
            profit_per_unit = entry - close

            if profit_per_unit > 0.0 and self._prev_resistance is not None and close > self._prev_resistance:
                self.BuyMarket()
                self._short_stop = None
                self._cooldown_remaining = int(self.SignalCooldownBars)
                return True

            self._update_short_trailing(close, entry)
        else:
            self._long_stop = None
            self._short_stop = None

        return False

    def _update_long_trailing(self, close_price, entry):
        if self._pip_size <= 0.0:
            return

        first_trigger = entry + 20.0 * self._pip_size
        second_trigger = entry + 40.0 * self._pip_size
        third_trigger = entry + 60.0 * self._pip_size

        first_stop = entry + 10.0 * self._pip_size
        second_stop = entry + 20.0 * self._pip_size
        third_stop = entry + 30.0 * self._pip_size

        if close_price > third_trigger and (self._long_stop is None or self._long_stop < third_stop):
            self._long_stop = third_stop
        elif close_price > second_trigger and (self._long_stop is None or self._long_stop < second_stop):
            self._long_stop = second_stop
        elif close_price > first_trigger and (self._long_stop is None or self._long_stop < first_stop):
            self._long_stop = first_stop

    def _update_short_trailing(self, close_price, entry):
        if self._pip_size <= 0.0:
            return

        first_trigger = entry - 20.0 * self._pip_size
        second_trigger = entry - 40.0 * self._pip_size
        third_trigger = entry - 60.0 * self._pip_size

        first_stop = entry - 10.0 * self._pip_size
        second_stop = entry - 20.0 * self._pip_size
        third_stop = entry - 30.0 * self._pip_size

        if close_price < third_trigger and (self._short_stop is None or self._short_stop > third_stop):
            self._short_stop = third_stop
        elif close_price < second_trigger and (self._short_stop is None or self._short_stop > second_stop):
            self._short_stop = second_stop
        elif close_price < first_trigger and (self._short_stop is None or self._short_stop > first_stop):
            self._short_stop = first_stop

    def OnReseted(self):
        super(support_resist_trade_strategy, self).OnReseted()
        self._prev_support = None
        self._prev_resistance = None
        self._long_stop = None
        self._short_stop = None
        self._trend = TREND_NONE
        self._pip_size = 0.0
        self._levels_initialized = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def CreateClone(self):
        return support_resist_trade_strategy()
