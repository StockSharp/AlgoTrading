import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ka_gold_bot_strategy(Strategy):
    def __init__(self):
        super(ka_gold_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._keltner_period = self.Param("KeltnerPeriod", 20)
        self._ema_short_period = self.Param("EmaShortPeriod", 10)
        self._ema_long_period = self.Param("EmaLongPeriod", 50)

        self._prev_ema_short = None
        self._prev_ema_long = None
        self._range_queue = []
        self._range_sum = 0.0
        self._keltner_ema = 0.0
        self._keltner_count = 0

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
    def EmaShortPeriod(self):
        return self._ema_short_period.Value

    @EmaShortPeriod.setter
    def EmaShortPeriod(self, value):
        self._ema_short_period.Value = value

    @property
    def EmaLongPeriod(self):
        return self._ema_long_period.Value

    @EmaLongPeriod.setter
    def EmaLongPeriod(self, value):
        self._ema_long_period.Value = value

    def OnReseted(self):
        super(ka_gold_bot_strategy, self).OnReseted()
        self._prev_ema_short = None
        self._prev_ema_long = None
        self._range_queue = []
        self._range_sum = 0.0
        self._keltner_ema = 0.0
        self._keltner_count = 0

    def OnStarted2(self, time):
        super(ka_gold_bot_strategy, self).OnStarted2(time)
        self._prev_ema_short = None
        self._prev_ema_long = None
        self._range_queue = []
        self._range_sum = 0.0
        self._keltner_ema = 0.0
        self._keltner_count = 0

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self.EmaShortPeriod
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self.EmaLongPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema_short, ema_long, self._process_candle).Start()

    def _process_candle(self, candle, ema_short_value, ema_long_value):
        if candle.State != CandleStates.Finished:
            return

        ema_short_val = float(ema_short_value)
        ema_long_val = float(ema_long_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Manual Keltner EMA
        keltner_period = self.KeltnerPeriod
        if self._keltner_count == 0:
            self._keltner_ema = close
        else:
            alpha = 2.0 / (keltner_period + 1)
            self._keltner_ema = close * alpha + self._keltner_ema * (1 - alpha)
        self._keltner_count += 1

        # Range average for Keltner bands
        bar_range = high - low
        self._range_queue.append(bar_range)
        self._range_sum += bar_range
        while len(self._range_queue) > keltner_period:
            self._range_sum -= self._range_queue.pop(0)

        if self._prev_ema_short is None or self._prev_ema_long is None:
            self._prev_ema_short = ema_short_val
            self._prev_ema_long = ema_long_val
            return

        # Buy: short EMA crosses above long EMA and close above Keltner center
        buy_signal = (self._prev_ema_short <= self._prev_ema_long and
                      ema_short_val > ema_long_val and close > self._keltner_ema)

        # Sell: short EMA crosses below long EMA and close below Keltner center
        sell_signal = (self._prev_ema_short >= self._prev_ema_long and
                       ema_short_val < ema_long_val and close < self._keltner_ema)

        if buy_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_ema_short = ema_short_val
        self._prev_ema_long = ema_long_val

    def CreateClone(self):
        return ka_gold_bot_strategy()
