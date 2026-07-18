import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ka_gold_bot_strategy(Strategy):
    def __init__(self):
        super(ka_gold_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._keltner_period = self.Param("KeltnerPeriod", 50)
        self._fast_ema_period = self.Param("FastEmaPeriod", 10)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 30)
        self._band_multiplier = self.Param("BandMultiplier", 3.0)

        self._keltner_ema_value = None
        self._range_buffer = []
        self._candle_count = 0
        self._prev_above_upper = False
        self._prev_below_lower = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def KeltnerPeriod(self):
        return self._keltner_period.Value

    @KeltnerPeriod.setter
    def KeltnerPeriod(self, value):
        self._keltner_period.Value = value

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
    def BandMultiplier(self):
        return self._band_multiplier.Value

    @BandMultiplier.setter
    def BandMultiplier(self, value):
        self._band_multiplier.Value = value

    def OnReseted(self):
        self._keltner_ema_value = None
        self._range_buffer = []
        self._candle_count = 0
        self._prev_above_upper = False
        self._prev_below_lower = False
        self._entry_price = 0.0
        super(ka_gold_bot_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(ka_gold_bot_strategy, self).OnStarted2(time)

        self._keltner_ema_value = None
        self._range_buffer = []
        self._candle_count = 0
        self._prev_above_upper = False
        self._prev_below_lower = False
        self._entry_price = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        candle_range = high - low
        period = self.KeltnerPeriod
        mult = float(self.BandMultiplier)

        # EMA for Keltner midline (manual computation)
        if self._keltner_ema_value is None:
            self._keltner_ema_value = close
        else:
            k = 2.0 / (period + 1.0)
            self._keltner_ema_value = close * k + self._keltner_ema_value * (1.0 - k)

        # SMA for range average (manual computation)
        self._range_buffer.append(candle_range)
        if len(self._range_buffer) > period:
            self._range_buffer.pop(0)

        self._candle_count += 1

        if self._candle_count < period:
            return

        mid = self._keltner_ema_value
        avg_range = sum(self._range_buffer) / len(self._range_buffer)
        upper = mid + avg_range * mult
        lower = mid - avg_range * mult

        above_upper = close > upper
        below_lower = close < lower

        fast_val = float(fast_value)
        slow_val = float(slow_value)

        # Exit logic: close crosses opposite band
        if self.Position > 0 and close < lower:
            self.SellMarket()
        elif self.Position < 0 and close > upper:
            self.BuyMarket()

        # Entry logic: Keltner breakout + EMA trend confirmation
        if self.Position == 0:
            if not self._prev_above_upper and above_upper and fast_val > slow_val:
                self.BuyMarket()
                self._entry_price = close
            elif not self._prev_below_lower and below_lower and fast_val < slow_val:
                self.SellMarket()
                self._entry_price = close

        self._prev_above_upper = above_upper
        self._prev_below_lower = below_lower

    def CreateClone(self):
        return ka_gold_bot_strategy()
