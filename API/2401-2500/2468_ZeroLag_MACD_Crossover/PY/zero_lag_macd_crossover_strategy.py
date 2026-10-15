import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zero_lag_macd_crossover_strategy(Strategy):
    def __init__(self):
        super(zero_lag_macd_crossover_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 5)
        self._slow_length = self.Param("SlowLength", 55)
        self._use_fresh_signal = self.Param("UseFreshSignal", True)
        self._start_hour = self.Param("StartHour", 9)
        self._end_hour = self.Param("EndHour", 15)
        self._kill_day = self.Param("KillDay", 5)
        self._kill_hour = self.Param("KillHour", 21)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_macd = 0.0
        self._prev_prev_macd = 0.0
        self._has_prev = False
        self._has_prev_prev = False

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
    def UseFreshSignal(self):
        return self._use_fresh_signal.Value

    @UseFreshSignal.setter
    def UseFreshSignal(self, value):
        self._use_fresh_signal.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @StartHour.setter
    def StartHour(self, value):
        self._start_hour.Value = value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @EndHour.setter
    def EndHour(self, value):
        self._end_hour.Value = value

    @property
    def KillDay(self):
        return self._kill_day.Value

    @KillDay.setter
    def KillDay(self, value):
        self._kill_day.Value = value

    @property
    def KillHour(self):
        return self._kill_hour.Value

    @KillHour.setter
    def KillHour(self, value):
        self._kill_hour.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(zero_lag_macd_crossover_strategy, self).OnStarted2(time)

        self._prev_macd = 0.0
        self._prev_prev_macd = 0.0
        self._has_prev = False
        self._has_prev_prev = False

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        self.StartProtection(None, None)

    def ProcessCandle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        t = candle.OpenTime

        start_h = int(self.StartHour)
        end_h = int(self.EndHour)
        kill_d = int(self.KillDay)
        kill_h = int(self.KillHour)

        if t.Hour < start_h or t.Hour >= end_h or (int(t.DayOfWeek) == kill_d and t.Hour == kill_h):
            pos = float(self.Position)
            if pos != 0:
                if pos > 0:
                    self.SellMarket(pos)
                else:
                    self.BuyMarket(-pos)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = 10.0 * (fast - slow)

        if not self._has_prev:
            self._prev_macd = macd
            self._has_prev = True
            return

        if not self._has_prev_prev:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd
            self._has_prev_prev = True
            return

        if self.UseFreshSignal:
            fresh = (self._prev_macd > self._prev_prev_macd and macd < self._prev_macd) or (self._prev_macd < self._prev_prev_macd and macd > self._prev_macd)
        else:
            fresh = True

        if not fresh:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd
            return

        pos = float(self.Position)
        vol = float(self.Volume)

        if macd > self._prev_macd:
            if pos > 0:
                self.SellMarket(pos)
                self._prev_prev_macd = self._prev_macd
                self._prev_macd = macd
                return
            if float(self.Position) == 0:
                self.SellMarket(vol)
        elif macd < self._prev_macd:
            if pos < 0:
                self.BuyMarket(-pos)
                self._prev_prev_macd = self._prev_macd
                self._prev_macd = macd
                return
            if float(self.Position) == 0:
                self.BuyMarket(vol)

        self._prev_prev_macd = self._prev_macd
        self._prev_macd = macd

    def OnReseted(self):
        super(zero_lag_macd_crossover_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_prev_macd = 0.0
        self._has_prev = False
        self._has_prev_prev = False

    def CreateClone(self):
        return zero_lag_macd_crossover_strategy()
