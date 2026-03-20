import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_barabashkakvn_edition_strategy(Strategy):
    def __init__(self):
        super(ema_barabashkakvn_edition_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1)
        self._virtual_profit_pips = self.Param("VirtualProfitPips", 5)
        self._move_back_pips = self.Param("MoveBackPips", 3)
        self._stop_loss_pips = self.Param("StopLossPips", 20)
        self._pip_size = self.Param("PipSize", 0.0001)
        self._fast_length = self.Param("FastLength", 5)
        self._slow_length = self.Param("SlowLength", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._has_cross_signal = False
        self._prev_fast = None
        self._prev_slow = None
        self._prev_high = None
        self._prev_low = None
        self._entry_price = None
        self._virtual_target = None
        self._virtual_stop = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @OrderVolume.setter
    def OrderVolume(self, value):
        self._order_volume.Value = value

    @property
    def VirtualProfitPips(self):
        return self._virtual_profit_pips.Value

    @VirtualProfitPips.setter
    def VirtualProfitPips(self, value):
        self._virtual_profit_pips.Value = value

    @property
    def MoveBackPips(self):
        return self._move_back_pips.Value

    @MoveBackPips.setter
    def MoveBackPips(self, value):
        self._move_back_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def PipSize(self):
        return self._pip_size.Value

    @PipSize.setter
    def PipSize(self, value):
        self._pip_size.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ema_barabashkakvn_edition_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowLength

        self._has_cross_signal = False
        self._prev_fast = None
        self._prev_slow = None
        self._prev_high = None
        self._prev_low = None
        self._entry_price = None
        self._virtual_target = None
        self._virtual_stop = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        median_price = (high + low) / 2.0

        fast_result = self._fast_ema.Process(self._fast_ema.CreateValue(candle.OpenTime, median_price))
        slow_result = self._slow_ema.Process(self._slow_ema.CreateValue(candle.OpenTime, median_price))

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            self._prev_high = high
            self._prev_low = low
            self._prev_fast = float(fast_result)
            self._prev_slow = float(slow_result)
            return

        fast = float(fast_result)
        slow = float(slow_result)

        if self._prev_fast is not None and self._prev_slow is not None:
            bullish_cross = self._prev_fast <= self._prev_slow and fast > slow
            bearish_cross = self._prev_fast >= self._prev_slow and fast < slow
            if bullish_cross or bearish_cross:
                self._has_cross_signal = True

        self._prev_fast = fast
        self._prev_slow = slow

        pip_value = float(self.PipSize)
        if pip_value <= 0.0:
            pip_value = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001

        move_back_price = int(self.MoveBackPips) * pip_value
        profit_distance = int(self.VirtualProfitPips) * pip_value
        stop_distance = int(self.StopLossPips) * pip_value

        if self.Position == 0 and self._has_cross_signal and self._prev_high is not None and self._prev_low is not None:
            bearish_spread = slow - fast
            bullish_spread = fast - slow

            bearish_ready = bearish_spread > 2.0 * pip_value and high >= self._prev_low + move_back_price
            bullish_ready = bullish_spread > 2.0 * pip_value and low <= self._prev_high - move_back_price

            if bearish_ready:
                self._entry_price = close
                self._virtual_target = self._entry_price - profit_distance
                self._virtual_stop = self._entry_price + stop_distance
                self.SellMarket()
                self._has_cross_signal = False
            elif bullish_ready:
                self._entry_price = close
                self._virtual_target = self._entry_price + profit_distance
                self._virtual_stop = self._entry_price - stop_distance
                self.BuyMarket()
                self._has_cross_signal = False

        elif self.Position != 0 and self._entry_price is not None and self._virtual_target is not None and self._virtual_stop is not None:
            if self.Position > 0:
                hit_target = high >= self._virtual_target
                hit_stop = low <= self._virtual_stop
                if hit_target or hit_stop:
                    self.SellMarket()
                    self._has_cross_signal = False
                    self._entry_price = None
                    self._virtual_target = None
                    self._virtual_stop = None
            elif self.Position < 0:
                hit_target = low <= self._virtual_target
                hit_stop = high >= self._virtual_stop
                if hit_target or hit_stop:
                    self.BuyMarket()
                    self._has_cross_signal = False
                    self._entry_price = None
                    self._virtual_target = None
                    self._virtual_stop = None

        if self.Position == 0:
            self._entry_price = None
            self._virtual_target = None
            self._virtual_stop = None

        self._prev_high = high
        self._prev_low = low

    def OnReseted(self):
        super(ema_barabashkakvn_edition_strategy, self).OnReseted()
        self._has_cross_signal = False
        self._prev_fast = None
        self._prev_slow = None
        self._prev_high = None
        self._prev_low = None
        self._entry_price = None
        self._virtual_target = None
        self._virtual_stop = None

    def CreateClone(self):
        return ema_barabashkakvn_edition_strategy()
