import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan


class channel_ea_limits_strategy(Strategy):
    def __init__(self):
        super(channel_ea_limits_strategy, self).__init__()

        self._begin_hour = self.Param("BeginHour", 1)
        self._end_hour = self.Param("EndHour", 10)
        self._order_volume = self.Param("OrderVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._session_high = None
        self._session_low = None
        self._bars_in_session = 0
        self._prev_candle_close = None
        self._orders_placed = False
        self._needs_session_reset = False
        self._trade_taken = False
        self._session_start = None
        self._session_end = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(channel_ea_limits_strategy, self).OnStarted2(time)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _calc_session_start(self, close_time):
        begin = self._begin_hour.Value
        end = self._end_hour.Value
        day = close_time.Date
        start = day.AddHours(begin)
        if begin <= end:
            if close_time < start:
                start = start.AddDays(-1)
        else:
            if close_time.TimeOfDay.TotalHours < begin:
                start = start.AddDays(-1)
        return start

    def _calc_session_end(self, session_start):
        end_hour = self._end_hour.Value
        begin_hour = self._begin_hour.Value
        day = session_start.Date
        end = day.AddHours(end_hour)
        if end_hour <= begin_hour or end <= session_start:
            end = end.AddDays(1)
        return end

    def _reset_session(self):
        self._session_high = None
        self._session_low = None
        self._bars_in_session = 0
        self._orders_placed = False
        self._needs_session_reset = True
        self._trade_taken = False

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close_time = candle.CloseTime
        session_start = self._calc_session_start(close_time)

        if self._session_start is None or self._session_start != session_start:
            self._session_start = session_start
            self._session_end = self._calc_session_end(session_start)
            self._reset_session()

        if self._needs_session_reset:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self._needs_session_reset = False

        open_time = candle.OpenTime
        if open_time >= self._session_start and open_time < self._session_end:
            h = float(candle.HighPrice)
            lo = float(candle.LowPrice)
            if self._session_high is None or h > self._session_high:
                self._session_high = h
            if self._session_low is None or lo < self._session_low:
                self._session_low = lo
            self._bars_in_session += 1

        if self._orders_placed and not self._trade_taken and self._bars_in_session >= 2:
            if self._session_low is not None and self._session_high is not None and self._session_low < self._session_high:
                if self.Position == 0:
                    if float(candle.LowPrice) <= self._session_low:
                        self.BuyMarket(self._order_volume.Value)
                        self._trade_taken = True
                    elif float(candle.HighPrice) >= self._session_high:
                        self.SellMarket(self._order_volume.Value)
                        self._trade_taken = True

        if not self._orders_placed and self._prev_candle_close is not None:
            if self._prev_candle_close < self._session_end and close_time >= self._session_end:
                if self._bars_in_session >= 2 and self._session_low is not None and self._session_high is not None and self._session_low < self._session_high:
                    self._orders_placed = True

        self._prev_candle_close = close_time

    def OnReseted(self):
        super(channel_ea_limits_strategy, self).OnReseted()
        self._session_high = None
        self._session_low = None
        self._bars_in_session = 0
        self._prev_candle_close = None
        self._orders_placed = False
        self._needs_session_reset = False
        self._trade_taken = False
        self._session_start = None
        self._session_end = None

    def CreateClone(self):
        return channel_ea_limits_strategy()
