import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xdpo_candle_strategy(Strategy):
    def __init__(self):
        super(xdpo_candle_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Length of the first EMA", "Parameters")
        self._slow_length = self.Param("SlowLength", 5) \
            .SetDisplay("Slow Length", "Length of the second EMA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_open1 = None
        self._ema_open2 = None
        self._ema_close1 = None
        self._ema_close2 = None
        self._previous_color = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xdpo_candle_strategy, self).OnReseted()
        self._ema_open1 = None
        self._ema_open2 = None
        self._ema_close1 = None
        self._ema_close2 = None
        self._previous_color = None

    def _calc_ema(self, price, prev, length):
        k = 2.0 / (length + 1.0)
        if prev is not None:
            result = price * k + prev * (1.0 - k)
        else:
            result = price
        return result

    def OnStarted(self, time):
        super(xdpo_candle_strategy, self).OnStarted(time)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.fast_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()

    def process_candle(self, candle, warmup_val):
        if candle.State != CandleStates.Finished:
            return
        fast_len = int(self.fast_length)
        slow_len = int(self.slow_length)
        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)
        open1 = self._calc_ema(open_price, self._ema_open1, fast_len)
        self._ema_open1 = open1
        open2 = self._calc_ema(open1, self._ema_open2, slow_len)
        self._ema_open2 = open2
        close1 = self._calc_ema(close_price, self._ema_close1, fast_len)
        self._ema_close1 = close1
        close2 = self._calc_ema(close1, self._ema_close2, slow_len)
        self._ema_close2 = close2
        if open2 < close2:
            color = 2
        elif open2 > close2:
            color = 0
        else:
            color = 1
        go_long = color == 2 and self._previous_color != 2
        go_short = color == 0 and self._previous_color != 0
        if go_long and self.Position <= 0:
            self.BuyMarket()
        elif go_short and self.Position >= 0:
            self.SellMarket()
        self._previous_color = color

    def CreateClone(self):
        return xdpo_candle_strategy()
