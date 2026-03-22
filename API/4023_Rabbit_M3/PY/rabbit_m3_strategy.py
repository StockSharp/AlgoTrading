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
)

class rabbit_m3_strategy(Strategy):
    def __init__(self):
        super(rabbit_m3_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 33) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter EMA", "Trend Filter")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 70) \
            .SetDisplay("Slow EMA Period", "Length of the slow trend filter EMA", "Trend Filter")
        self._williams_period = self.Param("WilliamsPeriod", 62) \
            .SetDisplay("Williams %R Period", "Lookback for Williams %R momentum", "Entry Filter")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -20.0) \
            .SetDisplay("Williams Sell Level", "Upper threshold crossed downward to trigger shorts", "Entry Filter")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -80.0) \
            .SetDisplay("Williams Buy Level", "Lower threshold crossed upward to trigger longs", "Entry Filter")
        self._cci_period = self.Param("CciPeriod", 26) \
            .SetDisplay("CCI Period", "Commodity Channel Index period", "Entry Filter")
        self._cci_sell_level = self.Param("CciSellLevel", 101.0) \
            .SetDisplay("CCI Sell Level", "Minimum CCI value required for short entries", "Entry Filter")
        self._cci_buy_level = self.Param("CciBuyLevel", 99.0) \
            .SetDisplay("CCI Buy Level", "Maximum CCI value allowed for long entries", "Entry Filter")
        self._donchian_length = self.Param("DonchianLength", 410) \
            .SetDisplay("Donchian Length", "History depth used for stop-and-reverse exits", "Risk")
        self._max_open_positions = self.Param("MaxOpenPositions", 1) \
            .SetDisplay("Max Open Positions", "Maximum simultaneous trades", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 360.0) \
            .SetDisplay("Take Profit (pips)", "Fixed profit target distance from entry", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 20.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance from entry", "Risk")
        self._entry_volume = self.Param("EntryVolume", 0.01) \
            .SetDisplay("Entry Volume", "Initial position size for each trade", "Money Management")
        self._big_win_threshold = self.Param("BigWinThreshold", 4.0) \
            .SetDisplay("Big Win Threshold", "Profit required to increase volume", "Money Management")
        self._volume_increment = self.Param("VolumeIncrement", 0.01) \
            .SetDisplay("Volume Increment", "Increment added to volume after beating Big Win Threshold", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for all indicators", "General")

        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_big_win_target = 0.0
        self._previous_williams = None
        self._trend_direction = 0
        self._allow_buy = False
        self._allow_sell = False
        self._long_active = False
        self._short_active = False
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop_price = 0.0
        self._long_take_profit_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_profit_price = 0.0
        self._high_history = []
        self._low_history = []
        self._prev_donchian_upper = None
        self._prev_donchian_lower = None

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @property
    def WilliamsSellLevel(self):
        return self._williams_sell_level.Value

    @property
    def WilliamsBuyLevel(self):
        return self._williams_buy_level.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def CciSellLevel(self):
        return self._cci_sell_level.Value

    @property
    def CciBuyLevel(self):
        return self._cci_buy_level.Value

    @property
    def DonchianLength(self):
        return self._donchian_length.Value

    @property
    def MaxOpenPositions(self):
        return self._max_open_positions.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def EntryVolume(self):
        return self._entry_volume.Value

    @property
    def BigWinThreshold(self):
        return self._big_win_threshold.Value

    @property
    def VolumeIncrement(self):
        return self._volume_increment.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(rabbit_m3_strategy, self).OnStarted(time)

        self._pip_size = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            self._pip_size = float(self.Security.PriceStep)
        if self._pip_size <= 0:
            self._pip_size = 1.0

        self._current_volume = float(self.EntryVolume)
        self.Volume = self._current_volume

        bwt = float(self.BigWinThreshold)
        vi = float(self.VolumeIncrement)
        self._current_big_win_target = bwt if bwt > 0 and vi > 0 else float('inf')

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastEmaPeriod
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowEmaPeriod
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._williams = WilliamsR()
        self._williams.Length = self.WilliamsPeriod

        self._high_history = []
        self._low_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._williams, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_value, slow_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        williams_val = float(williams_value)

        cci_result = self._cci.Process(candle)
        cci_val = float(cci_result) if cci_result is not None else 0.0

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._high_history.append(high)
        self._low_history.append(low)
        don_len = self.DonchianLength
        while len(self._high_history) > don_len:
            self._high_history.pop(0)
        while len(self._low_history) > don_len:
            self._low_history.pop(0)

        cur_upper = max(self._high_history) if len(self._high_history) > 0 else None
        cur_lower = min(self._low_history) if len(self._low_history) > 0 else None

        self._update_trend_state(fast_val, slow_val)

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._cci.IsFormed or not self._williams.IsFormed:
            self._previous_williams = williams_val
            self._prev_donchian_upper = cur_upper
            self._prev_donchian_lower = cur_lower
            return

        exit_upper = self._prev_donchian_upper
        exit_lower = self._prev_donchian_lower

        if exit_upper is None or exit_lower is None:
            self._previous_williams = williams_val
            self._prev_donchian_upper = cur_upper
            self._prev_donchian_lower = cur_lower
            return

        self._manage_exits(candle, exit_upper, exit_lower)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_williams = williams_val
            self._prev_donchian_upper = cur_upper
            self._prev_donchian_lower = cur_lower
            return

        self._try_enter_position(candle, cci_val, williams_val)

        self._previous_williams = williams_val
        self._prev_donchian_upper = cur_upper
        self._prev_donchian_lower = cur_lower

    def _update_trend_state(self, fast_value, slow_value):
        if fast_value < slow_value:
            if self._trend_direction == -1:
                return
            if self.Position > 0:
                self._close_long_position()
            self._allow_sell = True
            self._allow_buy = False
            self._trend_direction = -1
        elif fast_value > slow_value:
            if self._trend_direction == 1:
                return
            if self.Position < 0:
                self._close_short_position()
            self._allow_sell = False
            self._allow_buy = True
            self._trend_direction = 1

    def _manage_exits(self, candle, exit_upper, exit_lower):
        if self.Position < 0:
            if self._short_active:
                tp = float(self.TakeProfitPips)
                sl = float(self.StopLossPips)
                if tp > 0 and float(candle.LowPrice) <= self._short_take_profit_price:
                    self._close_short_position()
                    return
                if sl > 0 and float(candle.HighPrice) >= self._short_stop_price:
                    self._close_short_position()
                    return
            if float(candle.ClosePrice) >= exit_upper:
                self._close_short_position()

        elif self.Position > 0:
            if self._long_active:
                tp = float(self.TakeProfitPips)
                sl = float(self.StopLossPips)
                if tp > 0 and float(candle.HighPrice) >= self._long_take_profit_price:
                    self._close_long_position()
                    return
                if sl > 0 and float(candle.LowPrice) <= self._long_stop_price:
                    self._close_long_position()
                    return
            if float(candle.ClosePrice) <= exit_lower:
                self._close_long_position()

    def _try_enter_position(self, candle, cci_value, williams_value):
        if self.Position != 0:
            return

        if self._previous_williams is None:
            return

        if self.MaxOpenPositions <= 0:
            return

        prev_w = self._previous_williams
        sell_level = float(self.WilliamsSellLevel)
        buy_level = float(self.WilliamsBuyLevel)
        cci_sell = float(self.CciSellLevel)
        cci_buy = float(self.CciBuyLevel)
        close_price = float(candle.ClosePrice)
        pip = self._pip_size

        can_short = self._allow_sell and cci_value > cci_sell and prev_w > sell_level and prev_w < 0 and williams_value < sell_level
        if can_short:
            self._short_entry_price = close_price
            sl = float(self.StopLossPips)
            tp = float(self.TakeProfitPips)
            self._short_stop_price = close_price + sl * pip if sl > 0 else 0.0
            self._short_take_profit_price = close_price - tp * pip if tp > 0 else 0.0
            self._short_active = True
            self._long_active = False
            self.SellMarket(self._current_volume)
            return

        can_long = self._allow_buy and cci_value < cci_buy and prev_w < buy_level and prev_w < 0 and williams_value > buy_level
        if can_long:
            self._long_entry_price = close_price
            sl = float(self.StopLossPips)
            tp = float(self.TakeProfitPips)
            self._long_stop_price = close_price - sl * pip if sl > 0 else 0.0
            self._long_take_profit_price = close_price + tp * pip if tp > 0 else 0.0
            self._long_active = True
            self._short_active = False
            self.BuyMarket(self._current_volume)

    def _close_long_position(self):
        if self.Position <= 0:
            return
        self.SellMarket(self.Position)
        self._long_active = False
        self._long_entry_price = 0.0
        self._long_stop_price = 0.0
        self._long_take_profit_price = 0.0

    def _close_short_position(self):
        if self.Position >= 0:
            return
        self.BuyMarket(abs(self.Position))
        self._short_active = False
        self._short_entry_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_profit_price = 0.0

    def OnReseted(self):
        super(rabbit_m3_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_big_win_target = 0.0
        self._previous_williams = None
        self._trend_direction = 0
        self._allow_buy = False
        self._allow_sell = False
        self._long_active = False
        self._short_active = False
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop_price = 0.0
        self._long_take_profit_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_profit_price = 0.0
        self._high_history = []
        self._low_history = []
        self._prev_donchian_upper = None
        self._prev_donchian_lower = None

    def CreateClone(self):
        return rabbit_m3_strategy()
