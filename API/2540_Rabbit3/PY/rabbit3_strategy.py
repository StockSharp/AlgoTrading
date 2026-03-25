import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR, CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rabbit3_strategy(Strategy):
    def __init__(self):
        super(rabbit3_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._cci_period = self.Param("CciPeriod", 15)
        self._cci_buy_level = self.Param("CciBuyLevel", -80.0)
        self._cci_sell_level = self.Param("CciSellLevel", 80.0)
        self._williams_period = self.Param("WilliamsPeriod", 62)
        self._williams_oversold = self.Param("WilliamsOversold", -80.0)
        self._williams_overbought = self.Param("WilliamsOverbought", -20.0)
        self._fast_ema_period = self.Param("FastEmaPeriod", 17)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 30)
        self._max_positions = self.Param("MaxPositions", 2)
        self._profit_threshold = self.Param("ProfitThreshold", 4.0)
        self._base_volume = self.Param("BaseVolume", 0.01)
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.6)
        self._stop_loss_pips = self.Param("StopLossPips", 45)
        self._take_profit_pips = self.Param("TakeProfitPips", 110)

        self._previous_williams = 0.0
        self._has_prev_williams = False
        self._use_boost = False
        self._last_realized_pnl = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciBuyLevel(self):
        return self._cci_buy_level.Value

    @CciBuyLevel.setter
    def CciBuyLevel(self, value):
        self._cci_buy_level.Value = value

    @property
    def CciSellLevel(self):
        return self._cci_sell_level.Value

    @CciSellLevel.setter
    def CciSellLevel(self, value):
        self._cci_sell_level.Value = value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @WilliamsPeriod.setter
    def WilliamsPeriod(self, value):
        self._williams_period.Value = value

    @property
    def WilliamsOversold(self):
        return self._williams_oversold.Value

    @WilliamsOversold.setter
    def WilliamsOversold(self, value):
        self._williams_oversold.Value = value

    @property
    def WilliamsOverbought(self):
        return self._williams_overbought.Value

    @WilliamsOverbought.setter
    def WilliamsOverbought(self, value):
        self._williams_overbought.Value = value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @FastEmaPeriod.setter
    def FastEmaPeriod(self, value):
        self._fast_ema_period.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, value):
        self._slow_ema_period.Value = value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @MaxPositions.setter
    def MaxPositions(self, value):
        self._max_positions.Value = value

    @property
    def ProfitThreshold(self):
        return self._profit_threshold.Value

    @ProfitThreshold.setter
    def ProfitThreshold(self, value):
        self._profit_threshold.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    def OnStarted(self, time):
        super(rabbit3_strategy, self).OnStarted(time)

        self._previous_williams = 0.0
        self._has_prev_williams = False
        self._use_boost = False
        self.Volume = float(self.BaseVolume)
        self._last_realized_pnl = float(self.PnL)

        williams = WilliamsR()
        williams.Length = self.WilliamsPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        self._williams = williams
        self._cci = cci

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(williams, cci, fast_ema, slow_ema, self.ProcessCandle).Start()

        point = self._get_adjusted_point()
        tp_dist = int(self.TakeProfitPips) * point
        sl_dist = int(self.StopLossPips) * point

        self.StartProtection(
            Unit(sl_dist, UnitTypes.Absolute),
            Unit(tp_dist, UnitTypes.Absolute))

    def ProcessCandle(self, candle, williams_value, cci_value, fast_ema_value, slow_ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_volume_if_needed()

        w_val = float(williams_value)
        cci_val = float(cci_value)
        fast_val = float(fast_ema_value)
        slow_val = float(slow_ema_value)

        if not self._williams.IsFormed or not self._cci.IsFormed:
            self._previous_williams = w_val
            return

        if not self._has_prev_williams:
            self._previous_williams = w_val
            self._has_prev_williams = True
            return

        if w_val == 0.0:
            w_val = -1.0

        if self._previous_williams == 0.0:
            self._previous_williams = -1.0

        long_signal = (w_val < float(self.WilliamsOversold) and
                       cci_val < float(self.CciBuyLevel) and
                       fast_val > slow_val and
                       self._can_enter_long())

        short_signal = (w_val > float(self.WilliamsOverbought) and
                        cci_val > float(self.CciSellLevel) and
                        fast_val < slow_val and
                        self._can_enter_short())

        if long_signal:
            self.BuyMarket(self.Volume)
        elif short_signal:
            self.SellMarket(self.Volume)

        self._previous_williams = w_val

    def _update_volume_if_needed(self):
        realized_pnl = float(self.PnL)
        if realized_pnl != self._last_realized_pnl:
            delta = realized_pnl - self._last_realized_pnl
            self._use_boost = delta > float(self.ProfitThreshold)
            self._last_realized_pnl = realized_pnl
        self.Volume = self._get_trade_volume()

    def _can_enter_long(self):
        if float(self.Position) < 0.0:
            return False
        trade_volume = self._get_trade_volume()
        target_volume = float(self.Position) + trade_volume
        max_volume = int(self.MaxPositions) * trade_volume
        return target_volume <= max_volume + self._get_volume_tolerance()

    def _can_enter_short(self):
        if float(self.Position) > 0.0:
            return False
        trade_volume = self._get_trade_volume()
        target_volume = abs(float(self.Position) - trade_volume)
        max_volume = int(self.MaxPositions) * trade_volume
        return target_volume <= max_volume + self._get_volume_tolerance()

    def _get_trade_volume(self):
        multiplier = float(self.VolumeMultiplier) if self._use_boost else 1.0
        return float(self.BaseVolume) * multiplier

    def _get_adjusted_point(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        decimals = 0
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        adjust = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return step * adjust

    def _get_volume_tolerance(self):
        vol_step = None
        if self.Security is not None and self.Security.VolumeStep is not None:
            vol_step = float(self.Security.VolumeStep)
        if vol_step is None or vol_step == 0.0:
            return 0.00000001
        return vol_step / 2.0

    def OnReseted(self):
        super(rabbit3_strategy, self).OnReseted()
        self._previous_williams = 0.0
        self._has_prev_williams = False
        self._use_boost = False
        self._last_realized_pnl = 0.0

    def CreateClone(self):
        return rabbit3_strategy()
