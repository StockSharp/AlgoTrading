import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ozymandias_strategy(Strategy):

    def __init__(self):
        super(ozymandias_strategy, self).__init__()

        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "Lookback period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._trend = 0
        self._next_trend = 0
        self._maxl = 0.0
        self._minh = float('inf')
        self._base_line = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_direction = None
        self._candle_count = 0

        self._high_window = deque()
        self._low_window = deque()
        self._hh_queue = deque()
        self._hh_sum = 0.0
        self._ll_queue = deque()
        self._ll_sum = 0.0
        self._tr_queue = deque()
        self._tr_sum = 0.0
        self._prev_close = 0.0
        self._has_prev_close = False

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(ozymandias_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        length = self.Length
        atr_len = 14

        self._candle_count += 1

        self._high_window.append(high)
        if len(self._high_window) > length:
            self._high_window.popleft()

        self._low_window.append(low)
        if len(self._low_window) > length:
            self._low_window.popleft()

        if self._has_prev_close:
            tr = max(high - low, max(abs(high - self._prev_close), abs(low - self._prev_close)))
        else:
            tr = high - low

        self._tr_queue.append(tr)
        self._tr_sum += tr
        if len(self._tr_queue) > atr_len:
            self._tr_sum -= self._tr_queue.popleft()

        self._prev_close = close
        self._has_prev_close = True

        if len(self._high_window) < length:
            return

        hh = max(self._high_window)
        ll = min(self._low_window)

        self._hh_queue.append(hh)
        self._hh_sum += hh
        if len(self._hh_queue) > length:
            self._hh_sum -= self._hh_queue.popleft()

        self._ll_queue.append(ll)
        self._ll_sum += ll
        if len(self._ll_queue) > length:
            self._ll_sum -= self._ll_queue.popleft()

        if len(self._hh_queue) < length or len(self._tr_queue) < atr_len:
            return

        hma = self._hh_sum / len(self._hh_queue)
        lma = self._ll_sum / len(self._ll_queue)

        if self._prev_high == 0.0 and self._prev_low == 0.0:
            self._prev_high = high
            self._prev_low = low
            self._base_line = close
            return

        trend0 = self._trend

        if self._next_trend == 1:
            self._maxl = max(ll, self._maxl)
            if hma < self._maxl and close < self._prev_low:
                trend0 = 1
                self._next_trend = 0
                self._minh = hh

        if self._next_trend == 0:
            self._minh = min(hh, self._minh)
            if lma > self._minh and close > self._prev_high:
                trend0 = 0
                self._next_trend = 1
                self._maxl = ll

        if trend0 == 0:
            if self._trend != 0:
                pass
            else:
                self._base_line = max(self._maxl, self._base_line)
            direction = 1
        else:
            if self._trend != 1:
                pass
            else:
                self._base_line = min(self._minh, self._base_line)
            direction = 0

        if self._prev_direction is not None and direction != self._prev_direction:
            pos = self.Position
            if direction == 1 and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
            elif direction == 0 and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))

        self._prev_direction = direction
        self._trend = trend0
        self._prev_high = high
        self._prev_low = low

    def OnReseted(self):
        super(ozymandias_strategy, self).OnReseted()
        self._trend = 0
        self._next_trend = 0
        self._maxl = 0.0
        self._minh = float('inf')
        self._base_line = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_direction = None
        self._candle_count = 0
        self._high_window = deque()
        self._low_window = deque()
        self._hh_queue = deque()
        self._hh_sum = 0.0
        self._ll_queue = deque()
        self._ll_sum = 0.0
        self._tr_queue = deque()
        self._tr_sum = 0.0
        self._prev_close = 0.0
        self._has_prev_close = False

    def CreateClone(self):
        return ozymandias_strategy()
