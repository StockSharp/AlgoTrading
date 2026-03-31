import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    SimpleMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    DecimalIndicatorValue,
)


class exp_blau_cmi_strategy(Strategy):
    """Blau Candle Momentum Index: triple-smoothed momentum ratio with configurable price sources."""

    def __init__(self):
        super(exp_blau_cmi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for BlauCMI calculations", "General")
        # 0=EMA, 1=SMA, 2=Smoothed, 3=LWMA
        self._smoothing_method = self.Param("MomentumSmoothing", 0) \
            .SetDisplay("Smoothing Method", "Averaging mode for BlauCMI stages", "Indicator")
        self._momentum_length = self.Param("MomentumLength", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Depth", "Bars between compared prices", "Indicator")
        self._first_smoothing_length = self.Param("FirstSmoothingLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("First Smoothing", "Length of first BlauCMI smoothing", "Indicator")
        self._second_smoothing_length = self.Param("SecondSmoothingLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Second Smoothing", "Length of second BlauCMI smoothing", "Indicator")
        self._third_smoothing_length = self.Param("ThirdSmoothingLength", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Third Smoothing", "Length of third BlauCMI smoothing", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Shift", "Number of closed bars used for signals", "Trading")
        # Price types: 1=Close, 2=Open, 3=High, 4=Low, 5=Median, 6=Typical, 7=Weighted
        self._price_for_close = self.Param("PriceForClose", 1) \
            .SetDisplay("Momentum Price", "Price type for the leading leg", "Indicator")
        self._price_for_open = self.Param("PriceForOpen", 2) \
            .SetDisplay("Reference Price", "Price type compared against the delayed bar", "Indicator")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Allow Long Entry", "Enable opening long trades", "Trading")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Allow Short Entry", "Enable opening short trades", "Trading")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Enable closing long on opposite signals", "Trading")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Enable closing short on opposite signals", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop-Loss Points", "Distance to stop-loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take-Profit Points", "Distance to take-profit in price steps", "Risk")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Contract volume used for entries", "Trading")

        self._price_buffer = []
        self._indicator_history = []
        self._price_step = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def SmoothingMethod(self):
        return int(self._smoothing_method.Value)
    @property
    def MomentumLength(self):
        return int(self._momentum_length.Value)
    @property
    def FirstSmoothingLength(self):
        return int(self._first_smoothing_length.Value)
    @property
    def SecondSmoothingLength(self):
        return int(self._second_smoothing_length.Value)
    @property
    def ThirdSmoothingLength(self):
        return int(self._third_smoothing_length.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)
    @property
    def PriceForClose(self):
        return int(self._price_for_close.Value)
    @property
    def PriceForOpen(self):
        return int(self._price_for_open.Value)
    @property
    def AllowLongEntry(self):
        return self._allow_long_entry.Value
    @property
    def AllowShortEntry(self):
        return self._allow_short_entry.Value
    @property
    def AllowLongExit(self):
        return self._allow_long_exit.Value
    @property
    def AllowShortExit(self):
        return self._allow_short_exit.Value
    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)

    def _create_ma(self, method, length):
        n = max(1, length)
        if method == 1:
            ma = SimpleMovingAverage()
        elif method == 2:
            ma = SmoothedMovingAverage()
        elif method == 3:
            ma = WeightedMovingAverage()
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
        return c

    def OnStarted2(self, time):
        super(exp_blau_cmi_strategy, self).OnStarted2(time)

        self._price_buffer = []
        self._indicator_history = []

        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        sl_dist = self.StopLossPoints * self._price_step if self.StopLossPoints > 0 else 0.0
        tp_dist = self.TakeProfitPoints * self._price_step if self.TakeProfitPoints > 0 else 0.0

        tp = Unit(tp_dist, UnitTypes.Absolute) if tp_dist > 0 else None
        sl = Unit(sl_dist, UnitTypes.Absolute) if sl_dist > 0 else None
        if tp is not None and sl is not None:
            self.StartProtection(takeProfit=tp, stopLoss=sl)
        elif tp is not None:
            self.StartProtection(takeProfit=tp)
        elif sl is not None:
            self.StartProtection(stopLoss=sl)

        self._m_stage1 = self._create_ma(self.SmoothingMethod, self.FirstSmoothingLength)
        self._a_stage1 = self._create_ma(self.SmoothingMethod, self.FirstSmoothingLength)
        self._m_stage2 = self._create_ma(self.SmoothingMethod, self.SecondSmoothingLength)
        self._a_stage2 = self._create_ma(self.SmoothingMethod, self.SecondSmoothingLength)
        self._m_stage3 = self._create_ma(self.SmoothingMethod, self.ThirdSmoothingLength)
        self._a_stage3 = self._create_ma(self.SmoothingMethod, self.ThirdSmoothingLength)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        front_price = self._get_applied_price(candle, self.PriceForClose)
        ref_price = self._get_applied_price(candle, self.PriceForOpen)

        momentum_depth = max(1, self.MomentumLength)
        self._price_buffer.append(ref_price)
        while len(self._price_buffer) > momentum_depth:
            self._price_buffer.pop(0)

        if len(self._price_buffer) < momentum_depth:
            return

        delayed_price = self._price_buffer[0]
        momentum = front_price - delayed_price
        abs_momentum = abs(momentum)
        t = candle.ServerTime

        def _proc(ind, val):
            iv = DecimalIndicatorValue(ind, Decimal(val), t)
            iv.IsFinal = True
            return float(ind.Process(iv).Value)

        s1 = _proc(self._m_stage1, momentum)
        a1 = _proc(self._a_stage1, abs_momentum)

        s2 = _proc(self._m_stage2, s1)
        a2 = _proc(self._a_stage2, a1)

        s3_iv = DecimalIndicatorValue(self._m_stage3, Decimal(s2), t)
        s3_iv.IsFinal = True
        s3_val = self._m_stage3.Process(s3_iv)

        a3_iv = DecimalIndicatorValue(self._a_stage3, Decimal(a2), t)
        a3_iv.IsFinal = True
        a3_val = self._a_stage3.Process(a3_iv)

        if not s3_val.IsFormed or not a3_val.IsFormed:
            return

        denominator = float(a3_val.Value)
        if denominator == 0:
            return

        cmi = 100.0 * float(s3_val.Value) / denominator

        self._indicator_history.append(cmi)
        required = self.SignalBar + 3
        if len(self._indicator_history) > required:
            self._indicator_history = self._indicator_history[-required:]

        index = len(self._indicator_history) - 1 - self.SignalBar
        if index < 2:
            return

        v0 = self._indicator_history[index]
        v1 = self._indicator_history[index - 1]
        v2 = self._indicator_history[index - 2]

        buy_signal = v1 < v2 and v0 > v1
        sell_signal = v1 > v2 and v0 < v1

        if self.Position > 0 and self.AllowLongExit and sell_signal:
            self.SellMarket()

        if self.Position < 0 and self.AllowShortExit and buy_signal:
            self.BuyMarket()

        if self.Position != 0:
            return

        if buy_signal and self.AllowLongEntry:
            self.BuyMarket()
        elif sell_signal and self.AllowShortEntry:
            self.SellMarket()

    def OnReseted(self):
        super(exp_blau_cmi_strategy, self).OnReseted()
        self._price_buffer = []
        self._indicator_history = []
        self._price_step = 0.0

    def CreateClone(self):
        return exp_blau_cmi_strategy()
