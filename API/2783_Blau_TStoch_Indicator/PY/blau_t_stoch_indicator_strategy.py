import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    DecimalIndicatorValue, ExponentialMovingAverage,
    Highest, Lowest, SimpleMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class blau_t_stoch_indicator_strategy(Strategy):
    MODE_BREAKDOWN = 0
    MODE_TWIST = 1

    SMOOTH_EMA = 0
    SMOOTH_SMA = 1
    SMOOTH_SMMA = 2
    SMOOTH_LWMA = 3

    PRICE_CLOSE = 0
    PRICE_OPEN = 1
    PRICE_HIGH = 2
    PRICE_LOW = 3
    PRICE_MEDIAN = 4
    PRICE_TYPICAL = 5
    PRICE_WEIGHTED = 6
    PRICE_SIMPLE = 7
    PRICE_QUARTED = 8
    PRICE_TRENDFOLLOW0 = 9
    PRICE_TRENDFOLLOW1 = 10
    PRICE_DEMARK = 11

    def __init__(self):
        super(blau_t_stoch_indicator_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8)))
        self._smoothing_method = self.Param("Smoothing", self.SMOOTH_EMA)
        self._momentum_length = self.Param("MomentumLength", 5)
        self._first_smoothing = self.Param("FirstSmoothing", 5)
        self._second_smoothing = self.Param("SecondSmoothing", 8)
        self._third_smoothing = self.Param("ThirdSmoothing", 3)
        self._phase = self.Param("Phase", 15)
        self._price_type = self.Param("PriceType", self.PRICE_CLOSE)
        self._signal_bar = self.Param("SignalBar", 1)
        self._mode = self.Param("Mode", self.MODE_TWIST)
        self._allow_long_entries = self.Param("AllowLongEntries", True)
        self._allow_short_entries = self.Param("AllowShortEntries", True)
        self._allow_long_exits = self.Param("AllowLongExits", True)
        self._allow_short_exits = self.Param("AllowShortExits", True)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)

        self._indicator_values = []
        self._entry_price = 0.0

        self._highest = None
        self._lowest = None
        self._stoch1 = None
        self._stoch2 = None
        self._stoch3 = None
        self._range1 = None
        self._range2 = None
        self._range3 = None
        self._ind_formed = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Smoothing(self):
        return self._smoothing_method.Value

    @property
    def MomentumLength(self):
        return self._momentum_length.Value

    @property
    def FirstSmoothing(self):
        return self._first_smoothing.Value

    @property
    def SecondSmoothing(self):
        return self._second_smoothing.Value

    @property
    def ThirdSmoothing(self):
        return self._third_smoothing.Value

    @property
    def Phase(self):
        return self._phase.Value

    @property
    def PriceType(self):
        return self._price_type.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def Mode(self):
        return self._mode.Value

    @property
    def AllowLongEntries(self):
        return self._allow_long_entries.Value

    @property
    def AllowShortEntries(self):
        return self._allow_short_entries.Value

    @property
    def AllowLongExits(self):
        return self._allow_long_exits.Value

    @property
    def AllowShortExits(self):
        return self._allow_short_exits.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    def OnStarted(self, time):
        super(blau_t_stoch_indicator_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = max(1, self.MomentumLength)
        self._lowest = Lowest()
        self._lowest.Length = max(1, self.MomentumLength)

        self._stoch1 = self._create_smoother(self.FirstSmoothing)
        self._stoch2 = self._create_smoother(self.SecondSmoothing)
        self._stoch3 = self._create_smoother(self.ThirdSmoothing)
        self._range1 = self._create_smoother(self.FirstSmoothing)
        self._range2 = self._create_smoother(self.SecondSmoothing)
        self._range3 = self._create_smoother(self.ThirdSmoothing)
        self._ind_formed = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnOwnTradeReceived(self, trade):
        super(blau_t_stoch_indicator_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0.0:
            self._entry_price = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._handle_protective_levels(candle)

        ind_value = self._compute_indicator(candle)

        if not self._ind_formed:
            self._indicator_values.append(ind_value)
            self._trim_history()
            return

        self._indicator_values.append(ind_value)
        self._trim_history()

        max_offset = 2 if self.Mode == self.MODE_TWIST else 1
        if len(self._indicator_values) < self.SignalBar + max_offset:
            return

        value0 = self._get_shift_value(0)
        value1 = self._get_shift_value(1)
        value2 = self._get_shift_value(2) if max_offset >= 2 else 0.0

        open_long = False
        open_short = False
        close_long = False
        close_short = False

        if self.Mode == self.MODE_BREAKDOWN:
            if value1 > 0:
                if self.AllowLongEntries and value0 <= 0:
                    open_long = True
                if self.AllowShortExits:
                    close_short = True
            if value1 < 0:
                if self.AllowShortEntries and value0 >= 0:
                    open_short = True
                if self.AllowLongExits:
                    close_long = True
        elif self.Mode == self.MODE_TWIST:
            if value1 < value2:
                if self.AllowLongEntries and value0 >= value1:
                    open_long = True
                if self.AllowShortExits:
                    close_short = True
            if value1 > value2:
                if self.AllowShortEntries and value0 <= value1:
                    open_short = True
                if self.AllowLongExits:
                    close_long = True

        pos = float(self.Position)

        if close_long and pos > 0:
            self.SellMarket(pos)

        if close_short and pos < 0:
            self.BuyMarket(abs(pos))

        pos = float(self.Position)

        if open_long and pos <= 0:
            volume = float(self.Volume) + (abs(pos) if pos < 0 else 0.0)
            if volume > 0:
                self.BuyMarket(volume)

        if open_short and pos >= 0:
            volume = float(self.Volume) + (pos if pos > 0 else 0.0)
            if volume > 0:
                self.SellMarket(volume)

    def _handle_protective_levels(self, candle):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            return
        pos = float(self.Position)
        if pos > 0:
            if self.StopLossPoints > 0 and float(candle.LowPrice) <= self._entry_price - self.StopLossPoints * step:
                self.SellMarket(pos)
                return
            if self.TakeProfitPoints > 0 and float(candle.HighPrice) >= self._entry_price + self.TakeProfitPoints * step:
                self.SellMarket(pos)
                return
        elif pos < 0:
            ap = abs(pos)
            if self.StopLossPoints > 0 and float(candle.HighPrice) >= self._entry_price + self.StopLossPoints * step:
                self.BuyMarket(ap)
                return
            if self.TakeProfitPoints > 0 and float(candle.LowPrice) <= self._entry_price - self.TakeProfitPoints * step:
                self.BuyMarket(ap)
                return

    def _compute_indicator(self, candle):
        self._candle_time = candle.ServerTime
        price = self._get_price(candle)
        high_val = self._process_stage_ind(self._highest, float(candle.HighPrice))
        low_val = self._process_stage_ind(self._lowest, float(candle.LowPrice))

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            self._ind_formed = False
            return 0.0

        stoch = price - low_val
        rng = high_val - low_val

        s1 = self._process_stage(self._stoch1, stoch)
        r1 = self._process_stage(self._range1, rng)
        if not self._stoch1.IsFormed or not self._range1.IsFormed:
            self._ind_formed = False
            return 0.0

        s2 = self._process_stage(self._stoch2, s1)
        r2 = self._process_stage(self._range2, r1)
        if not self._stoch2.IsFormed or not self._range2.IsFormed:
            self._ind_formed = False
            return 0.0

        s3 = self._process_stage(self._stoch3, s2)
        r3 = self._process_stage(self._range3, r2)
        if not self._stoch3.IsFormed or not self._range3.IsFormed or r3 == 0.0:
            self._ind_formed = False
            return 0.0

        self._ind_formed = True
        return 100.0 * s3 / r3 - 50.0

    def _process_stage_ind(self, indicator, value):
        iv = DecimalIndicatorValue(indicator, Decimal(float(value)), self._candle_time)
        iv.IsFinal = True
        result = indicator.Process(iv)
        return float(result.Value)

    def _process_stage(self, indicator, value):
        iv = DecimalIndicatorValue(indicator, Decimal(float(value)), self._candle_time)
        iv.IsFinal = True
        result = indicator.Process(iv)
        return float(result.Value)

    def _trim_history(self):
        max_length = max(self.SignalBar + 5, 10)
        while len(self._indicator_values) > max_length:
            self._indicator_values.pop(0)

    def _get_shift_value(self, offset):
        index = len(self._indicator_values) - self.SignalBar - offset
        if index < 0 or index >= len(self._indicator_values):
            return 0.0
        return self._indicator_values[index]

    def _create_smoother(self, length):
        ln = max(1, length)
        if self.Smoothing == self.SMOOTH_SMA:
            ind = SimpleMovingAverage()
            ind.Length = ln
            return ind
        elif self.Smoothing == self.SMOOTH_SMMA:
            ind = SmoothedMovingAverage()
            ind.Length = ln
            return ind
        elif self.Smoothing == self.SMOOTH_LWMA:
            ind = WeightedMovingAverage()
            ind.Length = ln
            return ind
        else:
            ind = ExponentialMovingAverage()
            ind.Length = ln
            return ind

    def _get_price(self, candle):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        pt = self.PriceType

        if pt == self.PRICE_OPEN:
            return o
        elif pt == self.PRICE_HIGH:
            return h
        elif pt == self.PRICE_LOW:
            return l
        elif pt == self.PRICE_MEDIAN:
            return (h + l) / 2.0
        elif pt == self.PRICE_TYPICAL:
            return (c + h + l) / 3.0
        elif pt == self.PRICE_WEIGHTED:
            return (2.0 * c + h + l) / 4.0
        elif pt == self.PRICE_SIMPLE:
            return (o + c) / 2.0
        elif pt == self.PRICE_QUARTED:
            return (o + c + h + l) / 4.0
        elif pt == self.PRICE_TRENDFOLLOW0:
            if c > o:
                return h
            elif c < o:
                return l
            return c
        elif pt == self.PRICE_TRENDFOLLOW1:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (l + c) / 2.0
            return c
        elif pt == self.PRICE_DEMARK:
            return self._calculate_demark(o, h, l, c)
        return c

    def _calculate_demark(self, o, h, l, c):
        res = h + l + c
        if c < o:
            res = (res + l) / 2.0
        elif c > o:
            res = (res + h) / 2.0
        else:
            res = (res + c) / 2.0
        return ((res - l) + (res - h)) / 2.0

    def OnReseted(self):
        super(blau_t_stoch_indicator_strategy, self).OnReseted()
        self._indicator_values = []
        self._entry_price = 0.0
        self._ind_formed = False

    def CreateClone(self):
        return blau_t_stoch_indicator_strategy()
