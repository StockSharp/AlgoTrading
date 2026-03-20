import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_strategy(Strategy):
    EXIT_VWAP = 0
    EXIT_DEVIATION = 1
    EXIT_NONE = 2

    def __init__(self):
        super(vwap_strategy, self).__init__()
        self._stop_points = self.Param("StopPoints", 20.0) \
            .SetDisplay("Stop Points", "Stop buffer from signal bar", "Parameters")
        self._exit_mode_long = self.Param("ExitModeLong", 0) \
            .SetDisplay("Long Exit Mode", "", "Parameters")
        self._exit_mode_short = self.Param("ExitModeShort", 0) \
            .SetDisplay("Short Exit Mode", "", "Parameters")
        self._target_long_deviation = self.Param("TargetLongDeviation", 2.0) \
            .SetDisplay("Long Target Deviation", "", "Parameters")
        self._target_short_deviation = self.Param("TargetShortDeviation", 2.0) \
            .SetDisplay("Short Target Deviation", "", "Parameters")
        self._enable_safety_exit = self.Param("EnableSafetyExit", True) \
            .SetDisplay("Enable Safety Exit", "", "Parameters")
        self._num_opposing_bars = self.Param("NumOpposingBars", 3) \
            .SetDisplay("Opposing Bars", "", "Parameters")
        self._allow_longs = self.Param("AllowLongs", True) \
            .SetDisplay("Allow Longs", "", "Parameters")
        self._allow_shorts = self.Param("AllowShorts", True) \
            .SetDisplay("Allow Shorts", "", "Parameters")
        self._min_strength = self.Param("MinStrength", 0.7) \
            .SetDisplay("Min Strength", "", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._session_date = None
        self._sum_src = 0.0
        self._sum_vol = 0.0
        self._sum_src_sq_vol = 0.0
        self._signal_low = None
        self._signal_high = None
        self._bull_count = 0
        self._bear_count = 0

    @property
    def stop_points(self):
        return self._stop_points.Value

    @property
    def exit_mode_long(self):
        return self._exit_mode_long.Value

    @property
    def exit_mode_short(self):
        return self._exit_mode_short.Value

    @property
    def target_long_deviation(self):
        return self._target_long_deviation.Value

    @property
    def target_short_deviation(self):
        return self._target_short_deviation.Value

    @property
    def enable_safety_exit(self):
        return self._enable_safety_exit.Value

    @property
    def num_opposing_bars(self):
        return self._num_opposing_bars.Value

    @property
    def allow_longs(self):
        return self._allow_longs.Value

    @property
    def allow_shorts(self):
        return self._allow_shorts.Value

    @property
    def min_strength(self):
        return self._min_strength.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_strategy, self).OnReseted()
        self._session_date = None
        self._sum_src = 0.0
        self._sum_vol = 0.0
        self._sum_src_sq_vol = 0.0
        self._signal_low = None
        self._signal_high = None
        self._bull_count = 0
        self._bear_count = 0

    def OnStarted(self, time):
        super(vwap_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, dummy):
        if candle.State != CandleStates.Finished:
            return
        date = candle.OpenTime.Date
        vol = float(candle.TotalVolume)
        src = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        if self._session_date is None or date != self._session_date:
            self._session_date = date
            self._sum_src = src * vol
            self._sum_vol = vol
            self._sum_src_sq_vol = src * src * vol
        else:
            self._sum_src += src * vol
            self._sum_vol += vol
            self._sum_src_sq_vol += src * src * vol
        if self._sum_vol == 0:
            return
        vwap = self._sum_src / self._sum_vol
        variance = self._sum_src_sq_vol / self._sum_vol - vwap * vwap
        stdev = Math.Sqrt(float(max(variance, 0)))
        entry_upper = vwap + stdev * 2.0
        entry_lower = vwap - stdev * 2.0
        target_upper_long = vwap + stdev * self.target_long_deviation
        target_lower_short = vwap - stdev * self.target_short_deviation
        bar_range = float(candle.HighPrice) - float(candle.LowPrice)
        bull_strength = (float(candle.ClosePrice) - float(candle.LowPrice)) / bar_range if bar_range > 0 else 0.0
        bear_strength = (float(candle.HighPrice) - float(candle.ClosePrice)) / bar_range if bar_range > 0 else 0.0
        if candle.ClosePrice > candle.OpenPrice:
            self._bull_count += 1
            self._bear_count = 0
        elif candle.ClosePrice < candle.OpenPrice:
            self._bear_count += 1
            self._bull_count = 0
        else:
            self._bull_count = 0
            self._bear_count = 0
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        long_condition = self.allow_longs and opn < entry_lower and close > entry_lower and bull_strength >= self.min_strength and self.Position == 0
        short_condition = self.allow_shorts and opn > entry_upper and close < entry_upper and bear_strength >= self.min_strength and self.Position == 0
        if long_condition:
            self.BuyMarket()
            self._signal_low = low
            self._signal_high = None
        elif short_condition:
            self.SellMarket()
            self._signal_high = high
            self._signal_low = None
        if self.Position == 0:
            self._signal_low = None
            self._signal_high = None
        if self.Position > 0 and self._signal_low is not None:
            stop = self._signal_low - self.stop_points
            exit_vwap = self.exit_mode_long == self.EXIT_VWAP and high >= vwap
            exit_dev = self.exit_mode_long == self.EXIT_DEVIATION and high >= target_upper_long
            if low <= stop or (self.exit_mode_long != self.EXIT_NONE and (exit_vwap or exit_dev)):
                self.SellMarket()
            elif self.enable_safety_exit and self._bear_count >= self.num_opposing_bars:
                self.SellMarket()
        elif self.Position < 0 and self._signal_high is not None:
            stop = self._signal_high + self.stop_points
            exit_vwap = self.exit_mode_short == self.EXIT_VWAP and low <= vwap
            exit_dev = self.exit_mode_short == self.EXIT_DEVIATION and low <= target_lower_short
            if high >= stop or (self.exit_mode_short != self.EXIT_NONE and (exit_vwap or exit_dev)):
                self.BuyMarket()
            elif self.enable_safety_exit and self._bull_count >= self.num_opposing_bars:
                self.BuyMarket()

    def CreateClone(self):
        return vwap_strategy()
