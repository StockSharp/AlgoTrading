import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, Highest, Lowest, DecimalIndicatorValue, CandleIndicatorValue)
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal


class blau_ts_stochastic_strategy(Strategy):
    def __init__(self):
        super(blau_ts_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8)))
        self._base_length = self.Param("BaseLength", 5)
        self._smooth1 = self.Param("SmoothLength1", 10)
        self._smooth2 = self.Param("SmoothLength2", 5)
        self._smooth3 = self.Param("SmoothLength3", 3)
        self._signal_length = self.Param("SignalLength", 3)
        self._signal_bar = self.Param("SignalBar", 1)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)

        self._highest = None
        self._lowest = None
        self._stoch_s1 = None
        self._stoch_s2 = None
        self._stoch_s3 = None
        self._range_s1 = None
        self._range_s2 = None
        self._range_s3 = None
        self._signal_smooth = None
        self._hist_history = []
        self._signal_history = []
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _create_ma(self, length):
        ma = ExponentialMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(blau_ts_stochastic_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._base_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._base_length.Value
        self._stoch_s1 = self._create_ma(self._smooth1.Value)
        self._stoch_s2 = self._create_ma(self._smooth2.Value)
        self._stoch_s3 = self._create_ma(self._smooth3.Value)
        self._range_s1 = self._create_ma(self._smooth1.Value)
        self._range_s2 = self._create_ma(self._smooth2.Value)
        self._range_s3 = self._create_ma(self._smooth3.Value)
        self._signal_smooth = self._create_ma(self._signal_length.Value)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ_h = CandleIndicatorValue(self._highest, candle)
        civ_h.IsFinal = True
        high_result = self._highest.Process(civ_h)
        civ_l = CandleIndicatorValue(self._lowest, candle)
        civ_l.IsFinal = True
        low_result = self._lowest.Process(civ_l)

        if high_result.IsEmpty or low_result.IsEmpty or not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        # Manage SL/TP
        if self.Position != 0:
            step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
            if self.Position > 0:
                if self._stop_loss_points.Value > 0 and float(candle.LowPrice) <= self._entry_price - self._stop_loss_points.Value * step:
                    self.SellMarket(self.Position)
                    return
                if self._take_profit_points.Value > 0 and float(candle.HighPrice) >= self._entry_price + self._take_profit_points.Value * step:
                    self.SellMarket(self.Position)
                    return
            else:
                vol = abs(self.Position)
                if self._stop_loss_points.Value > 0 and float(candle.HighPrice) >= self._entry_price + self._stop_loss_points.Value * step:
                    self.BuyMarket(vol)
                    return
                if self._take_profit_points.Value > 0 and float(candle.LowPrice) <= self._entry_price - self._take_profit_points.Value * step:
                    self.BuyMarket(vol)
                    return

        t = candle.OpenTime
        high = float(high_result.Value)
        low = float(low_result.Value)
        price = float(candle.ClosePrice)
        stoch_raw = price - low
        range_raw = high - low

        div1 = DecimalIndicatorValue(self._stoch_s1, Decimal(float(stoch_raw)), t)
        div1.IsFinal = True
        s1 = self._stoch_s1.Process(div1)
        if s1.IsEmpty:
            return
        div2 = DecimalIndicatorValue(self._stoch_s2, Decimal(float(s1.Value)), t)
        div2.IsFinal = True
        s2 = self._stoch_s2.Process(div2)
        if s2.IsEmpty:
            return
        div3 = DecimalIndicatorValue(self._stoch_s3, Decimal(float(s2.Value)), t)
        div3.IsFinal = True
        s3 = self._stoch_s3.Process(div3)
        if s3.IsEmpty:
            return

        dir1 = DecimalIndicatorValue(self._range_s1, Decimal(float(range_raw)), t)
        dir1.IsFinal = True
        r1 = self._range_s1.Process(dir1)
        if r1.IsEmpty:
            return
        dir2 = DecimalIndicatorValue(self._range_s2, Decimal(float(r1.Value)), t)
        dir2.IsFinal = True
        r2 = self._range_s2.Process(dir2)
        if r2.IsEmpty:
            return
        dir3 = DecimalIndicatorValue(self._range_s3, Decimal(float(r2.Value)), t)
        dir3.IsFinal = True
        r3 = self._range_s3.Process(dir3)
        if r3.IsEmpty:
            return

        denom = float(r3.Value)
        if denom == 0:
            return

        hist = 200.0 * float(s3.Value) / denom - 100.0
        dsig = DecimalIndicatorValue(self._signal_smooth, Decimal(float(hist)), t)
        dsig.IsFinal = True
        sig_result = self._signal_smooth.Process(dsig)
        if sig_result.IsEmpty:
            return
        signal = float(sig_result.Value)

        self._hist_history.insert(0, hist)
        self._signal_history.insert(0, signal)
        sb = self._signal_bar.Value
        cap = max(sb + 3, 4)
        while len(self._hist_history) > cap:
            self._hist_history.pop()
        while len(self._signal_history) > cap:
            self._signal_history.pop()

        required = sb + 3
        if len(self._hist_history) < required:
            return

        hist_current = self._hist_history[sb]
        hist_prev = self._hist_history[sb + 1]
        hist_prev2 = self._hist_history[sb + 2]

        open_long = False
        open_short = False
        close_long = False
        close_short = False

        # Twist mode
        if hist_prev < hist_prev2:
            if hist_current > hist_prev:
                open_long = True
            close_short = True
        if hist_prev > hist_prev2:
            if hist_current < hist_prev:
                open_short = True
            close_long = True

        if close_long and self.Position > 0:
            self.SellMarket(self.Position)
        if close_short and self.Position < 0:
            self.BuyMarket(abs(self.Position))

        volume = float(self.Volume) + abs(self.Position)

        if open_long and self.Position <= 0:
            self.BuyMarket(volume)
            self._entry_price = float(candle.ClosePrice)
        elif open_short and self.Position >= 0:
            self.SellMarket(volume)
            self._entry_price = float(candle.ClosePrice)

    def OnReseted(self):
        super(blau_ts_stochastic_strategy, self).OnReseted()
        self._hist_history = []
        self._signal_history = []
        self._entry_price = 0.0

    def CreateClone(self):
        return blau_ts_stochastic_strategy()
