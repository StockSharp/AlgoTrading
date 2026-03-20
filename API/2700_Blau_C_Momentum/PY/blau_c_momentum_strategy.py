import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    SimpleMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    JurikMovingAverage,
    TripleExponentialMovingAverage,
    KaufmanAdaptiveMovingAverage,
    DecimalIndicatorValue,
)


class blau_c_momentum_strategy(Strategy):
    """Blau C-Momentum: triple-smoothed momentum with zero breakout or twist entry modes."""

    def __init__(self):
        super(blau_c_momentum_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading")
        # 0=Breakdown, 1=Twist
        self._entry_mode = self.Param("EntryMode", 1) \
            .SetDisplay("Entry Mode", "0=zero breakout, 1=twist logic", "Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Indicator Timeframe", "Candle type used for indicator calculations", "Data")
        # 0=SMA, 1=EMA, 2=Smoothed, 3=LWMA, 4=Jurik, 5=TripleEMA, 6=Kaufman
        self._smoothing_method = self.Param("SmoothingMethod", 1) \
            .SetDisplay("Smoothing Method", "Smoothing method applied to the momentum", "Indicator")
        self._momentum_length = self.Param("MomentumLength", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Length", "Depth of raw momentum calculation", "Indicator")
        self._first_smooth_length = self.Param("FirstSmoothLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("First Smooth", "Length of the first smoothing stage", "Indicator")
        self._second_smooth_length = self.Param("SecondSmoothLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Second Smooth", "Length of the second smoothing stage", "Indicator")
        self._third_smooth_length = self.Param("ThirdSmoothLength", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Third Smooth", "Length of the third smoothing stage", "Indicator")
        self._phase = self.Param("Phase", 15) \
            .SetDisplay("Phase", "Phase parameter used by Jurik-style moving averages", "Indicator")
        # Price types: 1=Close, 2=Open, 3=High, 4=Low, 5=Median, 6=Typical, 7=Weighted,
        #              8=Simple, 9=Quarter, 10=TrendFollow1, 11=TrendFollow2, 12=Demark
        self._price_for_close = self.Param("PriceForClose", 1) \
            .SetDisplay("Close Price Source", "Applied price used as the reference close", "Indicator")
        self._price_for_open = self.Param("PriceForOpen", 2) \
            .SetDisplay("Open Price Source", "Applied price used for the entry reference", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar index used for generating entry signals", "Logic")

        self._indicator_history = []
        self._price_buffer = []
        self._long_trade_block_until = None
        self._short_trade_block_until = None
        self._candle_span_ticks = 0

    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
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
    def EntryMode(self):
        return int(self._entry_mode.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def SmoothingMethod(self):
        return int(self._smoothing_method.Value)
    @property
    def MomentumLength(self):
        return max(1, int(self._momentum_length.Value))
    @property
    def FirstSmoothLength(self):
        return max(1, int(self._first_smooth_length.Value))
    @property
    def SecondSmoothLength(self):
        return max(1, int(self._second_smooth_length.Value))
    @property
    def ThirdSmoothLength(self):
        return max(1, int(self._third_smooth_length.Value))
    @property
    def Phase(self):
        return int(self._phase.Value)
    @property
    def PriceForClose(self):
        return int(self._price_for_close.Value)
    @property
    def PriceForOpen(self):
        return int(self._price_for_open.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)

    def _create_ma(self, method, length, phase):
        n = max(1, length)
        if method == 0:
            ma = SimpleMovingAverage()
        elif method == 2:
            ma = SmoothedMovingAverage()
        elif method == 3:
            ma = WeightedMovingAverage()
        elif method == 4:
            ma = JurikMovingAverage()
            ma.Length = n
            ma.Phase = phase
            return ma
        elif method == 5:
            ma = TripleExponentialMovingAverage()
        elif method == 6:
            ma = KaufmanAdaptiveMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = n
        return ma

    def _get_applied_price(self, candle, price_type):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if price_type == 1:
            return c
        elif price_type == 2:
            return o
        elif price_type == 3:
            return h
        elif price_type == 4:
            return lo
        elif price_type == 5:
            return (h + lo) / 2.0
        elif price_type == 6:
            return (c + h + lo) / 3.0
        elif price_type == 7:
            return (2.0 * c + h + lo) / 4.0
        elif price_type == 8:
            return (o + c) / 2.0
        elif price_type == 9:
            return (o + c + h + lo) / 4.0
        elif price_type == 10:
            if c > o:
                return h
            elif c < o:
                return lo
            return c
        elif price_type == 11:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (lo + c) / 2.0
            return c
        elif price_type == 12:
            return self._calc_demark(candle)
        return c

    def _calc_demark(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        res = h + lo + c
        if c < o:
            res = (res + lo) / 2.0
        elif c > o:
            res = (res + h) / 2.0
        else:
            res = (res + c) / 2.0
        return ((res - lo) + (res - h)) / 2.0

    def OnStarted(self, time):
        super(blau_c_momentum_strategy, self).OnStarted(time)

        self._indicator_history = []
        self._price_buffer = []
        self._long_trade_block_until = None
        self._short_trade_block_until = None

        self._ma1 = self._create_ma(self.SmoothingMethod, self.FirstSmoothLength, self.Phase)
        self._ma2 = self._create_ma(self.SmoothingMethod, self.SecondSmoothLength, self.Phase)
        self._ma3 = self._create_ma(self.SmoothingMethod, self.ThirdSmoothLength, self.Phase)

        self._candle_span_ticks = 0
        ct = self.CandleType
        if ct is not None and ct.Arg is not None:
            try:
                self._candle_span_ticks = int(ct.Arg.Ticks)
            except:
                pass

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        tp = Unit(self.TakeProfitPoints * step, UnitTypes.Absolute) if self.TakeProfitPoints > 0 and step > 0 else None
        sl = Unit(self.StopLossPoints * step, UnitTypes.Absolute) if self.StopLossPoints > 0 and step > 0 else None
        if tp is not None and sl is not None:
            self.StartProtection(takeProfit=tp, stopLoss=sl)
        elif tp is not None:
            self.StartProtection(takeProfit=tp)
        elif sl is not None:
            self.StartProtection(stopLoss=sl)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0

        indicator_value = self._process_momentum(candle, step)
        if indicator_value is None:
            return

        self._indicator_history.append(indicator_value)

        required = max(self.SignalBar + 3, 5)
        while len(self._indicator_history) > required:
            self._indicator_history.pop(0)

        current = self._get_history_value(self.SignalBar)
        previous = self._get_history_value(self.SignalBar + 1)

        if current is None or previous is None:
            return

        close_short = False
        close_long = False
        open_long = False
        open_short = False

        if self.EntryMode == 0:
            # Breakdown mode
            if previous > 0:
                if self.EnableLongEntry and current <= 0:
                    open_long = True
                if self.EnableShortExit:
                    close_short = True
            if previous < 0:
                if self.EnableShortEntry and current >= 0:
                    open_short = True
                if self.EnableLongExit:
                    close_long = True
        else:
            # Twist mode
            older = self._get_history_value(self.SignalBar + 2)
            if older is None:
                return

            if previous < older:
                if self.EnableLongEntry and current >= previous:
                    open_long = True
                if self.EnableShortExit:
                    close_short = True
            if previous > older:
                if self.EnableShortEntry and current <= previous:
                    open_short = True
                if self.EnableLongExit:
                    close_long = True

        if close_long and self.Position > 0:
            self.SellMarket()

        if close_short and self.Position < 0:
            self.BuyMarket()

        if open_long and self.Position <= 0 and self._can_enter_long(candle.OpenTime):
            self.BuyMarket()
            self._set_long_block(candle.OpenTime)

        if open_short and self.Position >= 0 and self._can_enter_short(candle.OpenTime):
            self.SellMarket()
            self._set_short_block(candle.OpenTime)

    def _process_momentum(self, candle, step):
        value1 = self._get_applied_price(candle, self.PriceForClose)
        value2 = self._get_applied_price(candle, self.PriceForOpen)

        self._price_buffer.append(value2)
        depth = self.MomentumLength
        while len(self._price_buffer) > depth:
            self._price_buffer.pop(0)

        if len(self._price_buffer) < depth:
            return None

        reference = self._price_buffer[0]
        momentum = value1 - reference
        t = candle.OpenTime

        s1_result = self._ma1.Process(DecimalIndicatorValue(self._ma1, momentum, t))
        if not self._ma1.IsFormed:
            return None
        s1 = float(s1_result.GetValue[float]())

        s2_result = self._ma2.Process(DecimalIndicatorValue(self._ma2, s1, t))
        if not self._ma2.IsFormed:
            return None
        s2 = float(s2_result.GetValue[float]())

        s3_result = self._ma3.Process(DecimalIndicatorValue(self._ma3, s2, t))
        if not self._ma3.IsFormed:
            return None
        s3 = float(s3_result.GetValue[float]())

        return s3 * 100.0 / step if step > 0 else s3

    def _get_history_value(self, shift):
        if shift < 0:
            return None
        index = len(self._indicator_history) - shift - 1
        if index < 0 or index >= len(self._indicator_history):
            return None
        return self._indicator_history[index]

    def _can_enter_long(self, signal_time):
        if self._long_trade_block_until is None:
            return True
        return signal_time >= self._long_trade_block_until

    def _can_enter_short(self, signal_time):
        if self._short_trade_block_until is None:
            return True
        return signal_time >= self._short_trade_block_until

    def _set_long_block(self, signal_time):
        if self._candle_span_ticks > 0:
            self._long_trade_block_until = signal_time.AddTicks(self._candle_span_ticks)
        else:
            self._long_trade_block_until = signal_time

    def _set_short_block(self, signal_time):
        if self._candle_span_ticks > 0:
            self._short_trade_block_until = signal_time.AddTicks(self._candle_span_ticks)
        else:
            self._short_trade_block_until = signal_time

    def OnReseted(self):
        super(blau_c_momentum_strategy, self).OnReseted()
        self._indicator_history = []
        self._price_buffer = []
        self._long_trade_block_until = None
        self._short_trade_block_until = None

    def CreateClone(self):
        return blau_c_momentum_strategy()
