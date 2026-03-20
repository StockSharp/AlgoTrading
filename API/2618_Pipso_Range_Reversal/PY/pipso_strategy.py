import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import Highest, Lowest


class pipso_strategy(Strategy):
    """Range-reversal strategy that fades breakouts of recent high/low range during a session."""

    def __init__(self):
        super(pipso_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._lookback = self.Param("LookbackPeriod", 36) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Number of candles for high/low extremes", "Channel")
        self._start_hour = self.Param("StartHour", 21) \
            .SetDisplay("Start Hour", "Session start hour (0-23)", "Session")
        self._end_hour = self.Param("EndHour", 9) \
            .SetDisplay("End Hour", "Session end hour (0-23)", "Session")
        self._stop_range_pct = self.Param("StopRangePercent", 300.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Range %", "Extra percentage of channel width for stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame used for calculations", "General")

        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._channel_init = False
        self._entry_price = None
        self._stop_price = None
        self._entry_side = None  # 'buy' or 'sell'

    @property
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def LookbackPeriod(self):
        return self._lookback.Value
    @property
    def StartHour(self):
        return self._start_hour.Value
    @property
    def EndHour(self):
        return self._end_hour.Value
    @property
    def StopRangePercent(self):
        return self._stop_range_pct.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(pipso_strategy, self).OnStarted(time)

        self.Volume = self.OrderVolume

        highest = Highest()
        highest.Length = self.LookbackPeriod
        lowest = Lowest()
        lowest.Length = self.LookbackPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        hv = float(highest_val)
        lv = float(lowest_val)

        if not self._channel_init:
            self._prev_highest = hv
            self._prev_lowest = lv
            self._channel_init = True
            return

        ch = self._prev_highest
        cl = self._prev_lowest

        self._manage_stop(candle)

        rng = ch - cl
        breakout_high = float(candle.HighPrice) >= ch and rng > 0
        breakout_low = float(candle.LowPrice) <= cl and rng > 0
        can_trade = self._in_window(candle.OpenTime)

        if breakout_high and self.Position > 0:
            self.SellMarket()
            self._reset_trade()

        if breakout_low and self.Position < 0:
            self.BuyMarket()
            self._reset_trade()

        if rng > 0:
            stop_dist = rng * (1.0 + float(self.StopRangePercent) / 100.0)

            if breakout_high and self.Position == 0 and can_trade:
                self.SellMarket()
                self._entry_side = 'sell'
                self._entry_price = ch
                self._stop_price = self._entry_price + stop_dist
            elif breakout_low and self.Position == 0 and can_trade:
                self.BuyMarket()
                self._entry_side = 'buy'
                self._entry_price = cl
                self._stop_price = self._entry_price - stop_dist

        if self.Position == 0:
            self._reset_trade()

        self._prev_highest = hv
        self._prev_lowest = lv

    def _manage_stop(self, candle):
        if self._entry_side is None or self._stop_price is None:
            return

        if self._entry_side == 'buy':
            if self.Position <= 0:
                self._reset_trade()
                return
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_trade()
        elif self._entry_side == 'sell':
            if self.Position >= 0:
                self._reset_trade()
                return
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_trade()

    def _in_window(self, time):
        ns = ((self.StartHour % 24) + 24) % 24
        ne = ((self.EndHour % 24) + 24) % 24
        if ns == ne:
            return False
        start = TimeSpan(ns, 0, 0)
        end = TimeSpan(ne, 0, 0)
        current = time.TimeOfDay
        if ns < ne:
            return current >= start and current <= end
        return current >= start or current <= end

    def _reset_trade(self):
        self._entry_price = None
        self._stop_price = None
        self._entry_side = None

    def OnReseted(self):
        super(pipso_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._channel_init = False
        self._reset_trade()

    def CreateClone(self):
        return pipso_strategy()
