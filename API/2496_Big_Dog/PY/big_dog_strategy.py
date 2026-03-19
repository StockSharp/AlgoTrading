import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class big_dog_strategy(Strategy):
    def __init__(self):
        super(big_dog_strategy, self).__init__()
        self._start_hour = self.Param("StartHour", 2) \
            .SetDisplay("Start Hour", "Hour to begin measuring the range", "Session")
        self._stop_hour = self.Param("StopHour", 8) \
            .SetDisplay("Stop Hour", "Hour to stop measuring the range", "Session")
        self._max_range_points = self.Param("MaxRangePoints", 50000.0) \
            .SetDisplay("Max Range", "Maximum allowed height of consolidation range", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 50.0) \
            .SetDisplay("Take Profit", "Take-profit distance in adjusted points", "Trading")
        self._distance_points = self.Param("DistancePoints", 1.0) \
            .SetDisplay("Min Distance", "Minimum distance from price to breakout level", "Trading")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Volume used for each breakout order", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles timeframe used for range detection", "Data")
        self._range_high = None
        self._range_low = None
        self._range_date = None
        self._long_ready = False
        self._short_ready = False
        self._long_stop_price = None
        self._long_tp_price = None
        self._short_stop_price = None
        self._short_tp_price = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._adjusted_point_size = 0.0

    @property
    def start_hour(self):
        return self._start_hour.Value
    @property
    def stop_hour(self):
        return self._stop_hour.Value
    @property
    def max_range_points(self):
        return self._max_range_points.Value
    @property
    def take_profit_points(self):
        return self._take_profit_points.Value
    @property
    def distance_points(self):
        return self._distance_points.Value
    @property
    def order_volume(self):
        return self._order_volume.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(big_dog_strategy, self).OnReseted()
        self._range_high = None
        self._range_low = None
        self._range_date = None
        self._long_ready = False
        self._short_ready = False
        self._long_stop_price = None
        self._long_tp_price = None
        self._short_stop_price = None
        self._short_tp_price = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._adjusted_point_size = 0.0

    def _calc_adjusted_point_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and sec.PriceStep > 0 else 1.0
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        multiplier = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return step * multiplier

    def _convert_to_price(self, points):
        return points * self._adjusted_point_size

    def OnStarted(self, time):
        super(big_dog_strategy, self).OnStarted(time)
        self._adjusted_point_size = self._calc_adjusted_point_size()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _reset_daily(self, date):
        self._range_date = date
        self._range_high = None
        self._range_low = None
        self._long_ready = False
        self._short_ready = False
        self._long_stop_price = None
        self._long_tp_price = None
        self._short_stop_price = None
        self._short_tp_price = None

    def _update_range(self, candle):
        hour = candle.OpenTime.Hour
        if hour < self.start_hour or hour >= self.stop_hour:
            return
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        self._range_high = max(self._range_high, h) if self._range_high is not None else h
        self._range_low = min(self._range_low, l) if self._range_low is not None else l

    def _prepare_breakout(self, candle):
        if self._range_high is None or self._range_low is None:
            return
        range_height = self._range_high - self._range_low
        max_range = self._convert_to_price(self.max_range_points)
        if range_height >= max_range:
            self._long_ready = False
            self._short_ready = False
            return
        min_dist = self._convert_to_price(self.distance_points)
        ask = float(candle.ClosePrice)
        bid = float(candle.ClosePrice)
        if not self._long_ready and self.Position >= 0 and (self._range_high - ask) > min_dist:
            self._long_ready = True
            self._long_entry_price = self._range_high
            self._long_stop_price = self._range_low
            self._long_tp_price = self._range_high + self._convert_to_price(self.take_profit_points)
        if not self._short_ready and self.Position <= 0 and (bid - self._range_low) > min_dist:
            self._short_ready = True
            self._short_entry_price = self._range_low
            self._short_stop_price = self._range_high
            self._short_tp_price = self._range_low - self._convert_to_price(self.take_profit_points)

    def _process_entries(self, candle):
        if self._long_ready and float(candle.HighPrice) >= self._long_entry_price and self.Position <= 0:
            self.BuyMarket()
            self._long_ready = False
            self._short_ready = False
        if self._short_ready and float(candle.LowPrice) <= self._short_entry_price and self.Position >= 0:
            self.SellMarket()
            self._short_ready = False
            self._long_ready = False

    def _process_risk(self, candle):
        if self.Position > 0 and self._long_stop_price is not None and self._long_tp_price is not None:
            if float(candle.LowPrice) <= self._long_stop_price:
                self.SellMarket()
                self._long_stop_price = None
                self._long_tp_price = None
            elif float(candle.HighPrice) >= self._long_tp_price:
                self.SellMarket()
                self._long_stop_price = None
                self._long_tp_price = None
        elif self.Position < 0 and self._short_stop_price is not None and self._short_tp_price is not None:
            if float(candle.HighPrice) >= self._short_stop_price:
                self.BuyMarket()
                self._short_stop_price = None
                self._short_tp_price = None
            elif float(candle.LowPrice) <= self._short_tp_price:
                self.BuyMarket()
                self._short_stop_price = None
                self._short_tp_price = None

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        current_date = candle.OpenTime.Date
        if self._range_date != current_date:
            self._reset_daily(current_date)

        self._update_range(candle)

        if candle.OpenTime.Hour >= self.stop_hour:
            self._prepare_breakout(candle)

        self._process_entries(candle)
        self._process_risk(candle)

    def CreateClone(self):
        return big_dog_strategy()
