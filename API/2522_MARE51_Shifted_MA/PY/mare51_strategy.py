import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mare51_strategy(Strategy):
    def __init__(self):
        super(mare51_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.01)
        self._take_profit_pips = self.Param("TakeProfitPips", 35.0)
        self._stop_loss_pips = self.Param("StopLossPips", 55.0)
        self._fast_period = self.Param("FastPeriod", 14)
        self._slow_period = self.Param("SlowPeriod", 20)
        self._ma_shift = self.Param("MovingAverageShift", 1)
        self._session_open_hour = self.Param("SessionOpenHour", 0)
        self._session_close_hour = self.Param("SessionCloseHour", 23)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._fast_buffer = None
        self._slow_buffer = None
        self._previous_candle_close = None
        self._previous_candle_open = None
        self._buffer_size = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def MovingAverageShift(self):
        return self._ma_shift.Value

    @MovingAverageShift.setter
    def MovingAverageShift(self, value):
        self._ma_shift.Value = value

    @property
    def SessionOpenHour(self):
        return self._session_open_hour.Value

    @SessionOpenHour.setter
    def SessionOpenHour(self, value):
        self._session_open_hour.Value = value

    @property
    def SessionCloseHour(self):
        return self._session_close_hour.Value

    @SessionCloseHour.setter
    def SessionCloseHour(self, value):
        self._session_close_hour.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(mare51_strategy, self).OnStarted(time)

        self._buffer_size = int(self.MovingAverageShift) + 6
        self._fast_buffer = [None] * self._buffer_size
        self._slow_buffer = [None] * self._buffer_size
        self._previous_candle_close = None
        self._previous_candle_open = None

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastPeriod
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowPeriod

        self._fast_sma = fast_sma
        self._slow_sma = slow_sma

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, self.ProcessCandle).Start()

        self.Volume = self._trade_volume.Value

        pip_size = self._calculate_pip_size()
        tp = float(self._take_profit_pips.Value)
        sl = float(self._stop_loss_pips.Value)
        take_unit = Unit(tp * pip_size, UnitTypes.Absolute) if tp > 0 else Unit(0)
        stop_unit = Unit(sl * pip_size, UnitTypes.Absolute) if sl > 0 else Unit(0)

        self.StartProtection(
            stopLoss=stop_unit,
            takeProfit=take_unit)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        for i in range(self._buffer_size - 1, 0, -1):
            self._fast_buffer[i] = self._fast_buffer[i - 1]
            self._slow_buffer[i] = self._slow_buffer[i - 1]

        self._fast_buffer[0] = fast_val
        self._slow_buffer[0] = slow_val

        prev_close = self._previous_candle_close
        prev_open = self._previous_candle_open
        self._previous_candle_close = float(candle.ClosePrice)
        self._previous_candle_open = float(candle.OpenPrice)

        if prev_close is None or prev_open is None:
            return

        if not self._fast_sma.IsFormed or not self._slow_sma.IsFormed:
            return

        shift = int(self.MovingAverageShift)
        f0 = self._get_shifted(self._fast_buffer, 0, shift)
        f2 = self._get_shifted(self._fast_buffer, 2, shift)
        f5 = self._get_shifted(self._fast_buffer, 5, shift)
        s0 = self._get_shifted(self._slow_buffer, 0, shift)
        s2 = self._get_shifted(self._slow_buffer, 2, shift)
        s5 = self._get_shifted(self._slow_buffer, 5, shift)

        if f0 is None or f2 is None or f5 is None or s0 is None or s2 is None or s5 is None:
            return

        hour = candle.OpenTime.Hour
        if hour < int(self.SessionOpenHour) or hour > int(self.SessionCloseHour):
            return

        bearish_prev = prev_close < prev_open
        bullish_prev = prev_close > prev_open

        sell_signal = f5 >= s5 and f2 < s2 and f0 < s0 and bearish_prev
        buy_signal = f5 <= s5 and f2 > s2 and f0 > s0 and bullish_prev

        if self.Position != 0:
            return

        if sell_signal:
            self.SellMarket()
        elif buy_signal:
            self.BuyMarket()

    def _get_shifted(self, buffer, index, shift):
        target = index + shift
        if target < 0 or target >= self._buffer_size:
            return None
        return buffer[target]

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 1.0
        ps = sec.PriceStep
        if ps is None:
            return 1.0
        step = float(ps)
        if step <= 0:
            return 1.0
        # count decimal places
        import System
        bits = System.Decimal.GetBits(ps)
        scale = (bits[3] >> 16) & 0xFF
        if scale == 3 or scale == 5:
            return step * 10.0
        return step

    def OnReseted(self):
        super(mare51_strategy, self).OnReseted()
        self._fast_buffer = None
        self._slow_buffer = None
        self._previous_candle_close = None
        self._previous_candle_open = None

    def CreateClone(self):
        return mare51_strategy()
