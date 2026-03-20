import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class time_zone_pivots_open_system_strategy(Strategy):
    def __init__(self):
        super(time_zone_pivots_open_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Timeframe", "General")
        self._offset_points = self.Param("OffsetPoints", 250.0) \
            .SetDisplay("Offset (points)", "Distance from anchor price in price steps", "Indicator")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start hour", "Hour whose opening price anchors the bands", "Indicator")
        self._signal_bar = self.Param("SignalBar", 2) \
            .SetDisplay("Signal bar", "Confirmation candle shift", "Signals")

        self._price_step = 0.0
        self._offset_distance = 0.0
        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = 0.0
        self._lower_zone = 0.0
        self._signal_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def OffsetPoints(self):
        return self._offset_points.Value
    @property
    def StartHour(self):
        h = self._start_hour.Value
        if h < 0:
            return 0
        if h > 23:
            return 23
        return h
    @property
    def SignalBar(self):
        v = self._signal_bar.Value
        return max(1, v)

    def OnReseted(self):
        super(time_zone_pivots_open_system_strategy, self).OnReseted()
        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = 0.0
        self._lower_zone = 0.0
        self._signal_history = []

    def OnStarted(self, time):
        super(time_zone_pivots_open_system_strategy, self).OnStarted(time)
        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if self._price_step <= 0.0:
            self._price_step = 1.0
        self._offset_distance = float(self.OffsetPoints) * self._price_step
        self._anchor_price = None
        self._anchor_date = None
        self._upper_zone = 0.0
        self._lower_zone = 0.0
        self._signal_history = []
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._offset_distance = float(self.OffsetPoints) * self._price_step
        self._update_anchor(candle)

        signal = self._calculate_signal(candle)
        self._record_signal(signal)

        if len(self._signal_history) <= self.SignalBar:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        confirm_index = self.SignalBar
        current_index = self.SignalBar - 1

        if current_index < 0 or confirm_index >= len(self._signal_history):
            return

        current_signal = self._signal_history[current_index]
        confirm_signal = self._signal_history[confirm_index]

        bullish_breakout = confirm_signal <= 1
        bearish_breakout = confirm_signal >= 3

        position = self.Position

        if position > 0 and bearish_breakout:
            self.SellMarket()
            position = self.Position

        if position < 0 and bullish_breakout:
            self.BuyMarket()
            position = self.Position

        if bullish_breakout and current_signal > 1 and position == 0:
            self.BuyMarket()
        elif bearish_breakout and current_signal < 3 and position == 0:
            self.SellMarket()

    def _update_anchor(self, candle):
        candle_date = candle.OpenTime.Date
        hour = candle.OpenTime.Hour

        if hour == self.StartHour and (self._anchor_date is None or self._anchor_date != candle_date):
            self._anchor_date = candle_date
            self._anchor_price = float(candle.OpenPrice)

        if self._anchor_price is not None:
            self._upper_zone = self._anchor_price + self._offset_distance
            self._lower_zone = self._anchor_price - self._offset_distance

    def _calculate_signal(self, candle):
        if self._anchor_price is None:
            return 2

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        if close > self._upper_zone:
            return 0 if close >= open_p else 1
        if close < self._lower_zone:
            return 4 if close <= open_p else 3
        return 2

    def _record_signal(self, signal):
        self._signal_history.insert(0, signal)
        max_cap = max(self.SignalBar + 2, 4)
        if len(self._signal_history) > max_cap:
            del self._signal_history[max_cap:]

    def CreateClone(self):
        return time_zone_pivots_open_system_strategy()
