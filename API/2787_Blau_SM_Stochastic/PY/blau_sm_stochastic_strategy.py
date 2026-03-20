import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    DecimalIndicatorValue, ExponentialMovingAverage,
    SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class blau_sm_stochastic_strategy(Strategy):
    MODE_BREAKDOWN = 0
    MODE_TWIST = 1
    MODE_CLOUD_TWIST = 2

    SMOOTH_SMA = 0
    SMOOTH_EMA = 1
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
    PRICE_QUARTER = 8
    PRICE_TRENDFOLLOW0 = 9
    PRICE_TRENDFOLLOW1 = 10
    PRICE_DEMARK = 11

    def __init__(self):
        super(blau_sm_stochastic_strategy, self).__init__()
        self._mode = self.Param("Mode", self.MODE_TWIST)
        self._signal_bar = self.Param("SignalBar", 1)
        self._lookback_length = self.Param("LookbackLength", 5)
        self._first_smoothing_length = self.Param("FirstSmoothingLength", 20)
        self._second_smoothing_length = self.Param("SecondSmoothingLength", 5)
        self._third_smoothing_length = self.Param("ThirdSmoothingLength", 3)
        self._signal_length = self.Param("SignalLength", 3)
        self._smooth_method = self.Param("SmoothMethod", self.SMOOTH_EMA)
        self._phase = self.Param("Phase", 15)
        self._price_type = self.Param("PriceType", self.PRICE_CLOSE)
        self._enable_long_entry = self.Param("EnableLongEntry", True)
        self._enable_short_entry = self.Param("EnableShortEntry", True)
        self._enable_long_exit = self.Param("EnableLongExit", True)
        self._enable_short_exit = self.Param("EnableShortExit", True)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8)))

        self._main_history = []
        self._signal_history = []
        self._entry_price = 0.0

        self._highs = []
        self._lows = []
        self._smooth1 = None
        self._smooth2 = None
        self._smooth3 = None
        self._half_smooth1 = None
        self._half_smooth2 = None
        self._half_smooth3 = None
        self._signal_smooth = None
        self._ind_formed = False
        self._last_signal = 0.0

    @property
    def Mode(self):
        return self._mode.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def LookbackLength(self):
        return self._lookback_length.Value

    @property
    def FirstSmoothingLength(self):
        return self._first_smoothing_length.Value

    @property
    def SecondSmoothingLength(self):
        return self._second_smoothing_length.Value

    @property
    def ThirdSmoothingLength(self):
        return self._third_smoothing_length.Value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @property
    def SmoothMethod(self):
        return self._smooth_method.Value

    @property
    def Phase(self):
        return self._phase.Value

    @property
    def PriceType(self):
        return self._price_type.Value

    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value

    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value

    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value

    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(blau_sm_stochastic_strategy, self).OnStarted(time)

        self._smooth1 = self._create_average(self.FirstSmoothingLength)
        self._smooth2 = self._create_average(self.SecondSmoothingLength)
        self._smooth3 = self._create_average(self.ThirdSmoothingLength)
        self._half_smooth1 = self._create_average(self.FirstSmoothingLength)
        self._half_smooth2 = self._create_average(self.SecondSmoothingLength)
        self._half_smooth3 = self._create_average(self.ThirdSmoothingLength)
        self._signal_smooth = self._create_average(self.SignalLength)
        self._ind_formed = False
        self._last_signal = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnOwnTradeReceived(self, trade):
        super(blau_sm_stochastic_strategy, self).OnOwnTradeReceived(trade)
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

        main, signal, formed = self._compute_indicator(candle)
        if not formed:
            return

        self._update_history(main, signal)

        has_current, current_main = self._try_get_main(self.SignalBar)
        has_previous, previous_main = self._try_get_main(self.SignalBar + 1)
        has_current_signal, current_signal = self._try_get_signal(self.SignalBar)
        has_previous_signal, previous_signal = self._try_get_signal(self.SignalBar + 1)

        if not has_current or not has_previous:
            return

        buy_entry = False
        sell_entry = False
        buy_exit = False
        sell_exit = False

        if self.Mode == self.MODE_BREAKDOWN:
            if previous_main > 0 and current_main <= 0:
                if self.EnableLongEntry:
                    buy_entry = True
                if self.EnableShortExit:
                    sell_exit = True
            if previous_main < 0 and current_main >= 0:
                if self.EnableShortEntry:
                    sell_entry = True
                if self.EnableLongExit:
                    buy_exit = True

        elif self.Mode == self.MODE_TWIST:
            has_older, older_main = self._try_get_main(self.SignalBar + 2)
            if not has_older:
                return
            if previous_main < older_main and current_main > previous_main:
                if self.EnableLongEntry:
                    buy_entry = True
                if self.EnableShortExit:
                    sell_exit = True
            if previous_main > older_main and current_main < previous_main:
                if self.EnableShortEntry:
                    sell_entry = True
                if self.EnableLongExit:
                    buy_exit = True

        elif self.Mode == self.MODE_CLOUD_TWIST:
            if not has_current_signal or not has_previous_signal:
                return
            if previous_main > previous_signal and current_main <= current_signal:
                if self.EnableLongEntry:
                    buy_entry = True
                if self.EnableShortExit:
                    sell_exit = True
            if previous_main < previous_signal and current_main >= current_signal:
                if self.EnableShortEntry:
                    sell_entry = True
                if self.EnableLongExit:
                    buy_exit = True

        pos = float(self.Position)

        if buy_exit and pos > 0:
            self.SellMarket(pos)
        if sell_exit and pos < 0:
            self.BuyMarket(abs(pos))

        pos = float(self.Position)

        if buy_entry and pos <= 0:
            self.BuyMarket(float(self.Volume) + abs(pos))
        if sell_entry and pos >= 0:
            self.SellMarket(float(self.Volume) + abs(pos))

    def _handle_protective_levels(self, candle):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
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
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)

        self._highs.append(h)
        self._lows.append(l)

        while len(self._highs) > self.LookbackLength:
            self._highs.pop(0)
        while len(self._lows) > self.LookbackLength:
            self._lows.pop(0)

        if len(self._highs) < self.LookbackLength or len(self._lows) < self.LookbackLength:
            return (0.0, 0.0, False)

        highest = max(self._highs)
        lowest = min(self._lows)
        price = self._get_applied_price(candle)

        sm = price - 0.5 * (lowest + highest)
        half = 0.5 * (highest - lowest)

        sm1 = self._process_stage(self._smooth1, sm)
        if sm1 is None:
            return (0.0, 0.0, False)
        sm2 = self._process_stage(self._smooth2, sm1)
        if sm2 is None:
            return (0.0, 0.0, False)
        sm3 = self._process_stage(self._smooth3, sm2)
        if sm3 is None:
            return (0.0, 0.0, False)

        h1 = self._process_stage(self._half_smooth1, half)
        if h1 is None:
            return (0.0, 0.0, False)
        h2 = self._process_stage(self._half_smooth2, h1)
        if h2 is None:
            return (0.0, 0.0, False)
        h3 = self._process_stage(self._half_smooth3, h2)
        if h3 is None or h3 == 0.0:
            return (0.0, 0.0, False)

        main = 100.0 * sm3 / h3

        sig = self._process_stage(self._signal_smooth, main)
        if sig is None:
            return (0.0, 0.0, False)

        self._last_signal = sig
        return (main, sig, True)

    def _process_stage(self, indicator, value):
        result = indicator.Process(DecimalIndicatorValue(indicator, value))
        if not indicator.IsFormed:
            return None
        try:
            return float(result.GetValue[float]())
        except:
            return None

    def _get_applied_price(self, candle):
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
        elif pt == self.PRICE_QUARTER:
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

    def _create_average(self, length):
        if self.SmoothMethod == self.SMOOTH_SMA:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind
        elif self.SmoothMethod == self.SMOOTH_SMMA:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif self.SmoothMethod == self.SMOOTH_LWMA:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind

    def _update_history(self, main, signal):
        self._main_history.append(main)
        self._signal_history.append(signal)
        mx = self.SignalBar + 5
        while len(self._main_history) > mx:
            self._main_history.pop(0)
        while len(self._signal_history) > mx:
            self._signal_history.pop(0)

    def _try_get_main(self, shift):
        index = len(self._main_history) - 1 - shift
        if index < 0:
            return (False, 0.0)
        return (True, self._main_history[index])

    def _try_get_signal(self, shift):
        index = len(self._signal_history) - 1 - shift
        if index < 0:
            return (False, 0.0)
        return (True, self._signal_history[index])

    def OnReseted(self):
        super(blau_sm_stochastic_strategy, self).OnReseted()
        self._main_history = []
        self._signal_history = []
        self._entry_price = 0.0
        self._highs = []
        self._lows = []
        self._smooth1 = None
        self._smooth2 = None
        self._smooth3 = None
        self._half_smooth1 = None
        self._half_smooth2 = None
        self._half_smooth3 = None
        self._signal_smooth = None
        self._ind_formed = False
        self._last_signal = 0.0

    def CreateClone(self):
        return blau_sm_stochastic_strategy()
