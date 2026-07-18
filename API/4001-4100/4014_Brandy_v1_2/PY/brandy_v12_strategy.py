import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class brandy_v12_strategy(Strategy):
    def __init__(self):
        super(brandy_v12_strategy, self).__init__()

        self._long_period = self.Param("LongPeriod", 70) \
            .SetDisplay("Long SMA Period", "Period for the longer moving average", "Indicators")
        self._long_shift = self.Param("LongShift", 5) \
            .SetDisplay("Long SMA Shift", "Backward shift applied to the longer SMA", "Indicators")
        self._short_period = self.Param("ShortPeriod", 20) \
            .SetDisplay("Short SMA Period", "Period for the shorter moving average", "Indicators")
        self._short_shift = self.Param("ShortShift", 5) \
            .SetDisplay("Short SMA Shift", "Backward shift applied to the shorter SMA", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 50.0) \
            .SetDisplay("Stop Loss (points)", "Initial stop-loss distance expressed in price steps", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 150.0) \
            .SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps. Activates when >= 100", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Candle series processed by the strategy", "General")

        self._long_history = []
        self._short_history = []
        self._entry_price = None
        self._stop_price = None

    @property
    def LongPeriod(self):
        return self._long_period.Value

    @property
    def LongShift(self):
        return self._long_shift.Value

    @property
    def ShortPeriod(self):
        return self._short_period.Value

    @property
    def ShortShift(self):
        return self._short_shift.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(brandy_v12_strategy, self).OnStarted2(time)

        self._long_sma = SimpleMovingAverage()
        self._long_sma.Length = self.LongPeriod
        self._short_sma = SimpleMovingAverage()
        self._short_sma.Length = self.ShortPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._long_sma, self._short_sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, long_value, short_value):
        if candle.State != CandleStates.Finished:
            return

        long_value = float(long_value)
        short_value = float(short_value)

        if not self._long_sma.IsFormed or not self._short_sma.IsFormed:
            return

        long_capacity = max(self.LongShift, 1) + 2
        short_capacity = max(self.ShortShift, 1) + 2
        self._update_history(self._long_history, long_value, long_capacity)
        self._update_history(self._short_history, short_value, short_capacity)

        long_prev = self._get_shifted(self._long_history, 1)
        long_shifted = self._get_shifted(self._long_history, self.LongShift)
        short_prev = self._get_shifted(self._short_history, 1)
        short_shifted = self._get_shifted(self._short_history, self.ShortShift)

        if long_prev is None or long_shifted is None or short_prev is None or short_shifted is None:
            return

        if self._manage_existing_position(candle, long_prev, long_shifted):
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position == 0:
            bullish = long_prev > long_shifted and short_prev > short_shifted
            bearish = long_prev < long_shifted and short_prev < short_shifted

            if bullish:
                self._enter_long(candle)
            elif bearish:
                self._enter_short(candle)

    def _manage_existing_position(self, candle, long_prev, long_shifted):
        if self.Position > 0:
            if long_prev < long_shifted:
                self.SellMarket(self.Position)
                self._reset_position_state()
                return True
            if self._update_long_stops(candle):
                self.SellMarket(self.Position)
                self._reset_position_state()
                return True
        elif self.Position < 0:
            if long_prev > long_shifted:
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_position_state()
                return True
            if self._update_short_stops(candle):
                self.BuyMarket(Math.Abs(self.Position))
                self._reset_position_state()
                return True
        return False

    def _enter_long(self, candle):
        vol = self.Volume
        if vol <= 0:
            return
        self.BuyMarket(vol)
        step = self._get_point()
        price = float(candle.ClosePrice)
        self._entry_price = price
        sl = float(self.StopLossPoints)
        self._stop_price = price - sl * step if sl > 0 else None

    def _enter_short(self, candle):
        vol = self.Volume
        if vol <= 0:
            return
        self.SellMarket(vol)
        step = self._get_point()
        price = float(candle.ClosePrice)
        self._entry_price = price
        sl = float(self.StopLossPoints)
        self._stop_price = price + sl * step if sl > 0 else None

    def _update_long_stops(self, candle):
        if self._entry_price is None:
            return False
        step = self._get_point()
        if step <= 0:
            return False
        entry = self._entry_price
        sl_pts = float(self.StopLossPoints)
        if self._stop_price is None and sl_pts > 0:
            self._stop_price = entry - sl_pts * step

        trail_pts = float(self.TrailingStopPoints)
        if trail_pts >= 100:
            trailing_dist = trail_pts * step
            if trailing_dist > 0:
                current_price = float(candle.ClosePrice)
                if current_price - entry > trailing_dist:
                    new_stop = current_price - trailing_dist
                    if self._stop_price is None or current_price - self._stop_price > trailing_dist:
                        self._stop_price = new_stop

        if self._stop_price is None:
            return False
        return float(candle.LowPrice) <= self._stop_price

    def _update_short_stops(self, candle):
        if self._entry_price is None:
            return False
        step = self._get_point()
        if step <= 0:
            return False
        entry = self._entry_price
        sl_pts = float(self.StopLossPoints)
        if self._stop_price is None and sl_pts > 0:
            self._stop_price = entry + sl_pts * step

        trail_pts = float(self.TrailingStopPoints)
        if trail_pts >= 100:
            trailing_dist = trail_pts * step
            if trailing_dist > 0:
                current_price = float(candle.ClosePrice)
                if entry - current_price > trailing_dist:
                    new_stop = current_price + trailing_dist
                    if self._stop_price is None or self._stop_price - current_price > trailing_dist:
                        self._stop_price = new_stop

        if self._stop_price is None:
            return False
        return float(candle.HighPrice) >= self._stop_price

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None

    def _update_history(self, history, value, capacity):
        history.append(value)
        while len(history) > capacity:
            history.pop(0)

    def _get_shifted(self, history, shift):
        if shift < 0:
            return None
        index = len(history) - 1 - shift
        if index < 0 or index >= len(history):
            return None
        return history[index]

    def _get_point(self):
        if self.Security is not None:
            ps = self.Security.PriceStep
            if ps is not None and float(ps) > 0:
                return float(ps)
        return 0.0001

    def OnReseted(self):
        super(brandy_v12_strategy, self).OnReseted()
        self._long_history = []
        self._short_history = []
        self._entry_price = None
        self._stop_price = None

    def CreateClone(self):
        return brandy_v12_strategy()
