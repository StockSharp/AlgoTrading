import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    WilliamsR,
    CommodityChannelIndex,
    Highest,
    Lowest,
)

class rabbit_m2_regime_swing_strategy(Strategy):
    def __init__(self):
        super(rabbit_m2_regime_swing_strategy, self).__init__()

        self._cci_sell_level = self.Param("CciSellLevel", 101) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._cci_buy_level = self.Param("CciBuyLevel", 99) \
            .SetDisplay("CCI Buy Level", "CCI threshold confirming a long signal", "CCI")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Lookback window for the Commodity Channel Index", "CCI")
        self._donchian_period = self.Param("DonchianPeriod", 100) \
            .SetDisplay("Donchian Period", "Length of the Donchian channel used for exits", "Donchian")
        self._max_trades = self.Param("MaxTrades", 1) \
            .SetDisplay("Max Trades", "Maximum number of base-volume units that can be open", "Risk")
        self._big_win_target = self.Param("BigWinTarget", 15.0) \
            .SetDisplay("Big Win Target", "Profit needed before the volume increases", "Money Management")
        self._volume_increment = self.Param("VolumeIncrement", 0.01) \
            .SetDisplay("Volume Increment", "How much to add to the base volume after a big win", "Money Management")
        self._wpr_period = self.Param("WprPeriod", 50) \
            .SetDisplay("Williams %R Period", "Length of the Williams %R oscillator", "Momentum")
        self._fast_ema_period = self.Param("FastEmaPeriod", 40) \
            .SetDisplay("Fast EMA Period", "Fast EMA period on the trend feed", "Trend")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 80) \
            .SetDisplay("Slow EMA Period", "Slow EMA period on the trend feed", "Trend")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("Take Profit (points)", "Distance from entry price to the take profit", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Stop Loss (points)", "Distance from entry price to the stop loss", "Risk")
        self._initial_volume = self.Param("InitialVolume", 0.01) \
            .SetDisplay("Initial Volume", "Starting base order size before scaling", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Primary Candle Type", "Timeframe for Williams %R, CCI and Donchian calculations", "General")

        self._base_volume = 0.0
        self._profit_threshold = 0.0
        self._last_realized_pnl = 0.0
        self._previous_wpr = None
        self._long_regime_enabled = False
        self._short_regime_enabled = False
        self._stop_distance = 0.0
        self._take_distance = 0.0
        self._active_stop = 0.0
        self._active_take = 0.0
        self._high_history = []
        self._low_history = []

    @property
    def CciSellLevel(self):
        return self._cci_sell_level.Value

    @property
    def CciBuyLevel(self):
        return self._cci_buy_level.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @property
    def BigWinTarget(self):
        return self._big_win_target.Value

    @property
    def VolumeIncrement(self):
        return self._volume_increment.Value

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calculate_price_offset(self, points):
        pts = int(points)
        if pts <= 0:
            return 0.0
        step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 0.0001
        decimals = None
        if self.Security is not None and self.Security.Decimals is not None:
            decimals = int(self.Security.Decimals)
        if decimals == 3 or decimals == 5:
            step *= 10.0
        return pts * step

    def _align_volume(self, volume):
        if self.Security is None or volume <= 0:
            return volume
        vs = self.Security.VolumeStep
        step = float(vs) if vs is not None and float(vs) > 0 else 0.0
        if step > 0:
            import math
            steps = math.floor(volume / step)
            volume = steps * step if steps > 0 else step
        min_v = self.Security.MinVolume
        if min_v is not None and float(min_v) > 0 and volume < float(min_v):
            volume = float(min_v)
        max_v = self.Security.MaxVolume
        if max_v is not None and float(max_v) > 0 and volume > float(max_v):
            volume = float(max_v)
        return volume

    def OnStarted(self, time):
        super(rabbit_m2_regime_swing_strategy, self).OnStarted(time)

        self._base_volume = float(self.InitialVolume)
        self._profit_threshold = float(self.BigWinTarget)
        self._last_realized_pnl = float(self.PnL)
        self._stop_distance = self._calculate_price_offset(self.StopLossPoints)
        self._take_distance = self._calculate_price_offset(self.TakeProfitPoints)

        self._base_volume = self._align_volume(self._base_volume)
        self.Volume = self._base_volume

        self._wpr = WilliamsR()
        self._wpr.Length = self.WprPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod

        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self.FastEmaPeriod
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self.SlowEmaPeriod

        self._high_history = []
        self._low_history = []

        trend_type = DataType.TimeFrame(TimeSpan.FromHours(2))
        trend_sub = self.SubscribeCandles(trend_type)
        trend_sub.Bind(self._ema_fast, self._ema_slow, self._process_trend).Start()

        primary_sub = self.SubscribeCandles(self.CandleType)
        primary_sub.Bind(self._wpr, self._process_primary).Start()

    def _process_trend(self, candle, fast_ema, slow_ema):
        if candle.State != CandleStates.Finished:
            return

        fast_ema = float(fast_ema)
        slow_ema = float(slow_ema)

        if fast_ema < slow_ema:
            self._short_regime_enabled = True
            self._long_regime_enabled = False
            self._close_long_position()
        elif fast_ema > slow_ema:
            self._long_regime_enabled = True
            self._short_regime_enabled = False
            self._close_short_position()

    def _process_primary(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr_val = float(wpr_value)

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._high_history.append(high)
        self._low_history.append(low)
        don_period = self.DonchianPeriod
        while len(self._high_history) > don_period:
            self._high_history.pop(0)
        while len(self._low_history) > don_period:
            self._low_history.pop(0)

        previous_upper = None
        previous_lower = None
        if len(self._high_history) >= 2:
            previous_upper = max(self._high_history[:-1]) if len(self._high_history) > 1 else None
        if len(self._low_history) >= 2:
            previous_lower = min(self._low_history[:-1]) if len(self._low_history) > 1 else None

        cci_result = self._cci.Process(candle)
        current_cci = float(cci_result.ToDecimal()) if cci_result is not None else 0.0

        self._handle_active_position(candle, previous_upper, previous_lower)

        current_wpr = wpr_val
        if current_wpr == 0:
            current_wpr = -1.0

        previous_wpr = self._previous_wpr

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_wpr = current_wpr
            return

        if previous_wpr is None:
            self._previous_wpr = current_wpr
            return

        wpr_lag = previous_wpr
        if wpr_lag == 0:
            wpr_lag = -1.0

        if self._short_regime_enabled:
            self._try_open_short(candle, current_wpr, wpr_lag, current_cci)

        if self._long_regime_enabled:
            self._try_open_long(candle, current_wpr, wpr_lag, current_cci)

        self._previous_wpr = current_wpr

    def _handle_active_position(self, candle, previous_upper, previous_lower):
        if self.Position > 0:
            if self._take_distance > 0 and self._active_take > 0 and float(candle.HighPrice) >= self._active_take:
                self._close_long_position()
            elif self._stop_distance > 0 and self._active_stop > 0 and float(candle.LowPrice) <= self._active_stop:
                self._close_long_position()
            elif previous_lower is not None and float(candle.ClosePrice) < previous_lower:
                self._close_long_position()
        elif self.Position < 0:
            if self._take_distance > 0 and self._active_take > 0 and float(candle.LowPrice) <= self._active_take:
                self._close_short_position()
            elif self._stop_distance > 0 and self._active_stop > 0 and float(candle.HighPrice) >= self._active_stop:
                self._close_short_position()
            elif previous_upper is not None and float(candle.ClosePrice) > previous_upper:
                self._close_short_position()

    def _try_open_short(self, candle, current_wpr, previous_wpr, current_cci):
        if not (current_wpr < -20 and previous_wpr > -20 and previous_wpr < 0 and current_cci > self.CciSellLevel):
            return

        if self._base_volume <= 0:
            return

        net_volume = abs(self.Position)
        max_volume = self._base_volume * self.MaxTrades
        if max_volume <= 0 or net_volume >= max_volume:
            return

        volume = min(self._base_volume, max_volume - net_volume)
        volume = self._align_volume(volume)
        if volume <= 0:
            return

        close_price = float(candle.ClosePrice)
        self.SellMarket(volume)
        self._active_stop = close_price + self._stop_distance if self._stop_distance > 0 else 0.0
        self._active_take = close_price - self._take_distance if self._take_distance > 0 else 0.0

    def _try_open_long(self, candle, current_wpr, previous_wpr, current_cci):
        if not (current_wpr > -80 and previous_wpr < -80 and previous_wpr < 0 and current_cci < self.CciBuyLevel):
            return

        if self._base_volume <= 0:
            return

        net_volume = abs(self.Position)
        max_volume = self._base_volume * self.MaxTrades
        if max_volume <= 0 or net_volume >= max_volume:
            return

        volume = min(self._base_volume, max_volume - net_volume)
        volume = self._align_volume(volume)
        if volume <= 0:
            return

        close_price = float(candle.ClosePrice)
        self.BuyMarket(volume)
        self._active_stop = close_price - self._stop_distance if self._stop_distance > 0 else 0.0
        self._active_take = close_price + self._take_distance if self._take_distance > 0 else 0.0

    def _close_long_position(self):
        volume = abs(self.Position)
        if volume <= 0:
            return
        self.SellMarket(volume)
        self._active_stop = 0.0
        self._active_take = 0.0

    def _close_short_position(self):
        volume = abs(self.Position)
        if volume <= 0:
            return
        self.BuyMarket(volume)
        self._active_stop = 0.0
        self._active_take = 0.0

    def OnOwnTradeReceived(self, trade):
        super(rabbit_m2_regime_swing_strategy, self).OnOwnTradeReceived(trade)

        realized_change = float(self.PnL) - self._last_realized_pnl
        self._last_realized_pnl = float(self.PnL)

        if realized_change > self._profit_threshold and float(self.VolumeIncrement) > 0:
            self._base_volume += float(self.VolumeIncrement)
            self._base_volume = self._align_volume(self._base_volume)
            self.Volume = self._base_volume
            if self._profit_threshold > 0:
                self._profit_threshold *= 2.0

        if abs(self.Position) == 0:
            self._active_stop = 0.0
            self._active_take = 0.0

    def OnReseted(self):
        super(rabbit_m2_regime_swing_strategy, self).OnReseted()
        self._base_volume = 0.0
        self._profit_threshold = 0.0
        self._last_realized_pnl = 0.0
        self._previous_wpr = None
        self._long_regime_enabled = False
        self._short_regime_enabled = False
        self._stop_distance = 0.0
        self._take_distance = 0.0
        self._active_stop = 0.0
        self._active_take = 0.0
        self._high_history = []
        self._low_history = []

    def CreateClone(self):
        return rabbit_m2_regime_swing_strategy()
