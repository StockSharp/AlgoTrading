import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class proper_bot_strategy(Strategy):
    def __init__(self):
        super(proper_bot_strategy, self).__init__()

        self._fast_period = self.Param("FastMaPeriod", 10)
        self._mid_period = self.Param("MidMaPeriod", 25)
        self._slow_period = self.Param("SlowMaPeriod", 50)
        self._disable_ma_filter = self.Param("DisableMaFilter", True)
        self._first_volume = self.Param("FirstVolume", 0.08)
        self._take_profit_points = self.Param("TakeProfitPoints", 10000)
        self._stop_loss_points = self.Param("StopLossPoints", 30000)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._price_step = 1.0
        self._has_previous_candle = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._previous_signal = 0
        self._entry_price = 0.0
        self._direction = 0

    @property
    def FastMaPeriod(self):
        return self._fast_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_period.Value = value

    @property
    def MidMaPeriod(self):
        return self._mid_period.Value

    @MidMaPeriod.setter
    def MidMaPeriod(self, value):
        self._mid_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_period.Value = value

    @property
    def DisableMaFilter(self):
        return self._disable_ma_filter.Value

    @DisableMaFilter.setter
    def DisableMaFilter(self, value):
        self._disable_ma_filter.Value = value

    @property
    def FirstVolume(self):
        return self._first_volume.Value

    @FirstVolume.setter
    def FirstVolume(self, value):
        self._first_volume.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(proper_bot_strategy, self).OnStarted2(time)

        self._has_previous_candle = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._previous_signal = 0
        self._entry_price = 0.0
        self._direction = 0

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._price_step <= 0.0:
            self._price_step = 1.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = max(1, int(self.FastMaPeriod))
        mid_ema = ExponentialMovingAverage()
        mid_ema.Length = max(1, int(self.MidMaPeriod))
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = max(1, int(self.SlowMaPeriod))

        self._fast_ema = fast_ema
        self._mid_ema = mid_ema
        self._slow_ema = slow_ema

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, mid_ema, slow_ema, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast_value, mid_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        mid_val = float(mid_value)
        slow_val = float(slow_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)

        if self.Position != 0:
            if self._manage_risk(candle):
                self._update_previous_candle(candle)
                return

        signal = self._calculate_signal(fast_val, mid_val, slow_val)

        if self.Position == 0 and signal != 0 and signal != self._previous_signal:
            if signal > 0:
                self.BuyMarket()
                self._entry_price = close
                self._direction = 1
            else:
                self.SellMarket()
                self._entry_price = close
                self._direction = -1

        self._update_previous_candle(candle)
        self._previous_signal = signal

    def _calculate_signal(self, fast_val, mid_val, slow_val):
        if self.DisableMaFilter:
            if not self._has_previous_candle:
                return 0
            if self._previous_close > self._previous_open:
                return 1
            if self._previous_close < self._previous_open:
                return -1
            return 0

        if not self._slow_ema.IsFormed:
            return 0

        if fast_val > slow_val:
            signal = 1
        elif fast_val < slow_val:
            signal = -1
        else:
            return 0

        mid_period = int(self.MidMaPeriod)
        if mid_period > 0 and self._mid_ema.IsFormed:
            if (mid_val >= fast_val and fast_val > slow_val) or (mid_val <= fast_val and fast_val < slow_val):
                return 0

        return signal

    def _manage_risk(self, candle):
        if self._entry_price <= 0.0:
            return False

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        tp_dist = int(self.TakeProfitPoints) * self._price_step
        sl_dist = int(self.StopLossPoints) * self._price_step

        if self._direction > 0:
            if tp_dist > 0.0 and high - self._entry_price >= tp_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._direction = 0
                return True
            if sl_dist > 0.0 and self._entry_price - low >= sl_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._direction = 0
                return True
        elif self._direction < 0:
            if tp_dist > 0.0 and self._entry_price - low >= tp_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._direction = 0
                return True
            if sl_dist > 0.0 and high - self._entry_price >= sl_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._direction = 0
                return True

        return False

    def _update_previous_candle(self, candle):
        self._previous_open = float(candle.OpenPrice)
        self._previous_close = float(candle.ClosePrice)
        self._has_previous_candle = True

    def OnReseted(self):
        super(proper_bot_strategy, self).OnReseted()
        self._price_step = 1.0
        self._has_previous_candle = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._previous_signal = 0
        self._entry_price = 0.0
        self._direction = 0

    def CreateClone(self):
        return proper_bot_strategy()
