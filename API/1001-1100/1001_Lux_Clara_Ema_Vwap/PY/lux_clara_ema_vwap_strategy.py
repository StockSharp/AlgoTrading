import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lux_clara_ema_vwap_strategy(Strategy):
    """
    Lux Clara EMA + VWAP strategy.
    Goes long when the fast EMA crosses above the slow EMA while the slow EMA is above the session VWAP, and short on the opposite
    cross with the slow EMA below VWAP. Entries are taken only between StartTime and EndTime (UTC). A position closes on the opposite
    EMA cross, which reverses it when the opposite entry conditions are also met.
    """

    def __init__(self):
        super(lux_clara_ema_vwap_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 8).SetGreaterThanZero().SetDisplay("Fast EMA Length", "Period of the fast EMA", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 50).SetGreaterThanZero().SetDisplay("Slow EMA Length", "Period of the slow EMA", "Indicators")
        self._start_time = self.Param("StartTime", TimeSpan(7, 30, 0)).SetDisplay("Start Time", "Session start time (UTC)", "Session")
        self._end_time = self.Param("EndTime", TimeSpan(14, 30, 0)).SetDisplay("End Time", "Session end time (UTC)", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._vwap_day = None
        self._cum_price_volume = Decimal(0)
        self._cum_volume = Decimal(0)

    def OnReseted(self):
        super(lux_clara_ema_vwap_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(lux_clara_ema_vwap_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        # VWAP is anchored to the start of each UTC day.
        day = candle.OpenTime.Date
        if self._vwap_day is None or self._vwap_day != day:
            self._vwap_day = day
            self._cum_price_volume = Decimal(0)
            self._cum_volume = Decimal(0)

        typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cum_price_volume += typical * candle.TotalVolume
        self._cum_volume += candle.TotalVolume

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None or self._cum_volume <= Decimal(0):
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        vwap = self._cum_price_volume / self._cum_volume
        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        time_of_day = candle.OpenTime.TimeOfDay
        in_session = time_of_day >= self._start_time.Value and time_of_day < self._end_time.Value

        if cross_up:
            if in_session and slow > vwap and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        elif cross_down:
            if in_session and slow < vwap and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return lux_clara_ema_vwap_strategy()
