import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class ka_gold_bot_strategy(Strategy):
    def __init__(self):
        super(ka_gold_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._keltner_period = self.Param("KeltnerPeriod", 50)
        self._fast_ema_period = self.Param("FastEmaPeriod", 10)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 200)

        self._close_prev1 = None
        self._close_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._slow_prev1 = None
        self._upper_prev1 = None
        self._upper_prev2 = None
        self._lower_prev1 = None
        self._lower_prev2 = None

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

    def OnReseted(self):
        super(ka_gold_bot_strategy, self).OnReseted()
        self._close_prev1 = None
        self._close_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._slow_prev1 = None
        self._upper_prev1 = None
        self._upper_prev2 = None
        self._lower_prev1 = None
        self._lower_prev2 = None

    def OnStarted2(self, time):
        super(ka_gold_bot_strategy, self).OnStarted2(time)
        self._close_prev1 = None
        self._close_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._slow_prev1 = None
        self._upper_prev1 = None
        self._upper_prev2 = None
        self._lower_prev1 = None
        self._lower_prev2 = None

        self._keltner_ema = ExponentialMovingAverage()
        self._keltner_ema.Length = self.KeltnerPeriod
        self._range_average = SimpleMovingAverage()
        self._range_average.Length = self.KeltnerPeriod

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _can_evaluate_signals(self):
        return (self._close_prev1 is not None and self._close_prev2 is not None and
                self._fast_prev1 is not None and self._fast_prev2 is not None and
                self._slow_prev1 is not None and
                self._upper_prev1 is not None and self._upper_prev2 is not None and
                self._lower_prev1 is not None and self._lower_prev2 is not None)

    def _is_buy_signal(self):
        if not self._can_evaluate_signals():
            return False
        entry_buy1 = self._close_prev1 > self._upper_prev1
        entry_buy2 = self._close_prev1 > self._slow_prev1
        entry_buy3 = self._fast_prev2 < self._upper_prev2 and self._fast_prev1 > self._upper_prev1
        return entry_buy1 and entry_buy2 and entry_buy3

    def _is_sell_signal(self):
        if not self._can_evaluate_signals():
            return False
        entry_sell1 = self._close_prev1 < self._lower_prev1
        entry_sell2 = self._close_prev1 < self._slow_prev1
        entry_sell3 = self._fast_prev2 > self._lower_prev2 and self._fast_prev1 < self._lower_prev1
        return entry_sell1 and entry_sell2 and entry_sell3

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        candle_range = high - low

        # Manually process Keltner EMA and range average
        mid_input = DecimalIndicatorValue(self._keltner_ema, close, candle.OpenTime)
        mid_result = self._keltner_ema.Process(mid_input)
        range_input = DecimalIndicatorValue(self._range_average, candle_range, candle.OpenTime)
        range_result = self._range_average.Process(range_input)

        upper = None
        lower = None

        if self._keltner_ema.IsFormed and self._range_average.IsFormed:
            mid = float(mid_result.GetValue[float]())
            avg_range = float(range_result.GetValue[float]())
            upper = mid + avg_range
            lower = mid - avg_range

            # Check entry signals
            if self._can_evaluate_signals() and self.Position == 0:
                if self._is_buy_signal():
                    self.BuyMarket()
                elif self._is_sell_signal():
                    self.SellMarket()

        # Update history
        self._close_prev2 = self._close_prev1
        self._close_prev1 = close
        self._fast_prev2 = self._fast_prev1
        self._fast_prev1 = fast_val
        self._slow_prev1 = slow_val
        self._upper_prev2 = self._upper_prev1
        self._upper_prev1 = upper
        self._lower_prev2 = self._lower_prev1
        self._lower_prev1 = lower

    def CreateClone(self):
        return ka_gold_bot_strategy()
