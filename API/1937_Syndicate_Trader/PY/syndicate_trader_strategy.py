import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class syndicate_trader_strategy(Strategy):

    def __init__(self):
        super(syndicate_trader_strategy, self).__init__()

        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA length", "General")
        self._slow_ema_length = self.Param("SlowEmaLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA length", "General")
        self._volume_ma_length = self.Param("VolumeMaLength", 20) \
            .SetDisplay("Volume MA", "Volume MA length", "General")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.8) \
            .SetDisplay("Volume Mult", "Volume spike multiplier", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 1200.0) \
            .SetDisplay("Take Profit", "Take profit in price points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 700.0) \
            .SetDisplay("Stop Loss", "Stop loss in price points", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._use_session_filter = self.Param("UseSessionFilter", False) \
            .SetDisplay("Use Session", "Enable session filter", "Session")
        self._session_start_hour = self.Param("SessionStartHour", 0) \
            .SetDisplay("Start Hour", "Session start hour", "Session")
        self._session_start_minute = self.Param("SessionStartMinute", 0) \
            .SetDisplay("Start Minute", "Session start minute", "Session")
        self._session_end_hour = self.Param("SessionEndHour", 23) \
            .SetDisplay("End Hour", "Session end hour", "Session")
        self._session_end_minute = self.Param("SessionEndMinute", 59) \
            .SetDisplay("End Minute", "Session end minute", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._fast_ema = ExponentialMovingAverage()
        self._slow_ema = ExponentialMovingAverage()
        self._volume_ma = SimpleMovingAverage()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def FastEmaLength(self):
        return self._fast_ema_length.Value

    @FastEmaLength.setter
    def FastEmaLength(self, value):
        self._fast_ema_length.Value = value

    @property
    def SlowEmaLength(self):
        return self._slow_ema_length.Value

    @SlowEmaLength.setter
    def SlowEmaLength(self, value):
        self._slow_ema_length.Value = value

    @property
    def VolumeMaLength(self):
        return self._volume_ma_length.Value

    @VolumeMaLength.setter
    def VolumeMaLength(self, value):
        self._volume_ma_length.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volume_multiplier.Value = value

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
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def UseSessionFilter(self):
        return self._use_session_filter.Value

    @UseSessionFilter.setter
    def UseSessionFilter(self, value):
        self._use_session_filter.Value = value

    @property
    def SessionStartHour(self):
        return self._session_start_hour.Value

    @SessionStartHour.setter
    def SessionStartHour(self, value):
        self._session_start_hour.Value = value

    @property
    def SessionStartMinute(self):
        return self._session_start_minute.Value

    @SessionStartMinute.setter
    def SessionStartMinute(self, value):
        self._session_start_minute.Value = value

    @property
    def SessionEndHour(self):
        return self._session_end_hour.Value

    @SessionEndHour.setter
    def SessionEndHour(self, value):
        self._session_end_hour.Value = value

    @property
    def SessionEndMinute(self):
        return self._session_end_minute.Value

    @SessionEndMinute.setter
    def SessionEndMinute(self, value):
        self._session_end_minute.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(syndicate_trader_strategy, self).OnStarted(time)

        self._fast_ema.Length = self.FastEmaLength
        self._slow_ema.Length = self.SlowEmaLength
        self._volume_ma.Length = self.VolumeMaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPoints, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPoints, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.UseSessionFilter:
            time_of_day = candle.OpenTime.TimeOfDay
            start = TimeSpan(self.SessionStartHour, self.SessionStartMinute, 0)
            end = TimeSpan(self.SessionEndHour, self.SessionEndMinute, 0)
            if time_of_day < start or time_of_day > end:
                return

        fi = DecimalIndicatorValue(self._fast_ema, candle.ClosePrice, candle.OpenTime)
        fi.IsFinal = True
        fast = float(self._fast_ema.Process(fi))
        si = DecimalIndicatorValue(self._slow_ema, candle.ClosePrice, candle.OpenTime)
        si.IsFinal = True
        slow = float(self._slow_ema.Process(si))
        vi = DecimalIndicatorValue(self._volume_ma, candle.TotalVolume, candle.OpenTime)
        vi.IsFinal = True
        volume_avg = float(self._volume_ma.Process(vi))

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._volume_ma.IsFormed:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return

        cross_up = fast > slow and self._prev_fast <= self._prev_slow
        cross_down = fast < slow and self._prev_fast >= self._prev_slow
        has_volume_spike = float(candle.TotalVolume) >= volume_avg * float(self.VolumeMultiplier)

        if self._bars_since_trade >= self.CooldownBars and has_volume_spike:
            pos = self.Position
            if cross_up and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif cross_down and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_fast = fast
        self._prev_slow = slow

    def OnReseted(self):
        super(syndicate_trader_strategy, self).OnReseted()
        self._fast_ema.Length = self.FastEmaLength
        self._slow_ema.Length = self.SlowEmaLength
        self._volume_ma.Length = self.VolumeMaLength
        self._fast_ema.Reset()
        self._slow_ema.Reset()
        self._volume_ma.Reset()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return syndicate_trader_strategy()
