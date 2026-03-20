import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class heiken_ashi_waves_strategy(Strategy):
    def __init__(self):
        super(heiken_ashi_waves_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 2)
        self._slow_length = self.Param("SlowLength", 30)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._is_initialized = False

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
        super(heiken_ashi_waves_strategy, self).OnStarted(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._is_initialized = False

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastLength
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        if not self._is_initialized:
            ha_open = (o + c) / 2.0
            ha_close = (o + h + l + c) / 4.0
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return

        ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
        ha_close = (o + h + l + c) / 4.0

        is_bullish = ha_close > ha_open
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        if is_bullish and cross_up and self.Position <= 0:
            self.BuyMarket()
        elif not is_bullish and cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

    def OnReseted(self):
        super(heiken_ashi_waves_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._is_initialized = False

    def CreateClone(self):
        return heiken_ashi_waves_strategy()
