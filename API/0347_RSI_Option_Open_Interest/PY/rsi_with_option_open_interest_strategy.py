import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class rsi_with_option_open_interest_strategy(Strategy):
    """
    RSI strategy filtered by deterministic option open-interest spikes.
    """

    def __init__(self):
        super(rsi_with_option_open_interest_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._oi_period = self.Param("OiPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("OI Period", "Period for open interest averaging", "Options")

        self._oi_deviation_factor = self.Param("OiDeviationFactor", 2.5) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("OI StdDev Factor", "Standard deviation multiplier for OI threshold", "Options")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 18) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._rsi = None
        self._call_oi_sma = None
        self._put_oi_sma = None
        self._call_oi_std = None
        self._put_oi_std = None

        self._current_call_oi = 0.0
        self._current_put_oi = 0.0
        self._avg_call_oi = 0.0
        self._avg_put_oi = 0.0
        self._std_call_oi = 0.0
        self._std_put_oi = 0.0
        self._prev_rsi = None
        self._prev_call_oi_spike = False
        self._prev_put_oi_spike = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(rsi_with_option_open_interest_strategy, self).OnReseted()
        self._rsi = None
        self._call_oi_sma = None
        self._put_oi_sma = None
        self._call_oi_std = None
        self._put_oi_std = None
        self._current_call_oi = 0.0
        self._current_put_oi = 0.0
        self._avg_call_oi = 0.0
        self._avg_put_oi = 0.0
        self._std_call_oi = 0.0
        self._std_put_oi = 0.0
        self._prev_rsi = None
        self._prev_call_oi_spike = False
        self._prev_put_oi_spike = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(rsi_with_option_open_interest_strategy, self).OnStarted(time)

        oi_period = int(self._oi_period.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_period.Value)

        self._call_oi_sma = SimpleMovingAverage()
        self._call_oi_sma.Length = oi_period

        self._call_oi_std = StandardDeviation()
        self._call_oi_std.Length = oi_period

        self._put_oi_sma = SimpleMovingAverage()
        self._put_oi_sma.Length = oi_period

        self._put_oi_std = StandardDeviation()
        self._put_oi_std.Length = oi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        self.SimulateOptionOi(candle)

        rsi_val = float(rsi)

        call_sma_iv = DecimalIndicatorValue(self._call_oi_sma, self._current_call_oi, candle.OpenTime)
        call_sma_iv.IsFinal = True
        call_sma_result = self._call_oi_sma.Process(call_sma_iv)

        put_sma_iv = DecimalIndicatorValue(self._put_oi_sma, self._current_put_oi, candle.OpenTime)
        put_sma_iv.IsFinal = True
        put_sma_result = self._put_oi_sma.Process(put_sma_iv)

        call_std_iv = DecimalIndicatorValue(self._call_oi_std, self._current_call_oi, candle.OpenTime)
        call_std_iv.IsFinal = True
        call_std_result = self._call_oi_std.Process(call_std_iv)

        put_std_iv = DecimalIndicatorValue(self._put_oi_std, self._current_put_oi, candle.OpenTime)
        put_std_iv.IsFinal = True
        put_std_result = self._put_oi_std.Process(put_std_iv)

        if not self._call_oi_sma.IsFormed or not self._put_oi_sma.IsFormed or \
           not self._call_oi_std.IsFormed or not self._put_oi_std.IsFormed or \
           call_sma_result.IsEmpty or put_sma_result.IsEmpty or \
           call_std_result.IsEmpty or put_std_result.IsEmpty:
            self._prev_rsi = rsi_val
            return

        self._avg_call_oi = float(call_sma_result)
        self._avg_put_oi = float(put_sma_result)
        self._std_call_oi = float(call_std_result)
        self._std_put_oi = float(put_std_result)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        oi_dev = float(self._oi_deviation_factor.Value)
        cooldown = int(self._cooldown_bars.Value)

        call_oi_threshold = self._avg_call_oi + oi_dev * self._std_call_oi
        put_oi_threshold = self._avg_put_oi + oi_dev * self._std_put_oi
        call_oi_spike = self._current_call_oi > call_oi_threshold
        put_oi_spike = self._current_put_oi > put_oi_threshold
        call_oi_spike_transition = (not self._prev_call_oi_spike) and call_oi_spike
        put_oi_spike_transition = (not self._prev_put_oi_spike) and put_oi_spike

        oversold_cross = self._prev_rsi is not None and self._prev_rsi >= 35.0 and rsi_val < 35.0
        overbought_cross = self._prev_rsi is not None and self._prev_rsi <= 65.0 and rsi_val > 65.0

        if self._cooldown_remaining == 0 and oversold_cross and call_oi_spike_transition and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and overbought_cross and put_oi_spike_transition and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and rsi_val >= 52.0:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rsi_val <= 48.0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_rsi = rsi_val
        self._prev_call_oi_spike = call_oi_spike
        self._prev_put_oi_spike = put_oi_spike

    def SimulateOptionOi(self, candle):
        range_val = max(float(candle.HighPrice - candle.LowPrice), 1.0)
        body = float(candle.ClosePrice - candle.OpenPrice)
        body_ratio = abs(body) / range_val
        range_ratio = range_val / max(float(candle.OpenPrice), 1.0)
        base_oi = max(float(candle.TotalVolume), 1.0)
        spike_factor = 1.0 + min(0.75, (body_ratio * 0.5) + (range_ratio * 20.0))

        if body >= 0:
            self._current_call_oi = base_oi * spike_factor
            self._current_put_oi = base_oi * (0.75 + (1.0 - body_ratio) * 0.25)
        else:
            self._current_call_oi = base_oi * (0.75 + (1.0 - body_ratio) * 0.25)
            self._current_put_oi = base_oi * spike_factor

    def CreateClone(self):
        return rsi_with_option_open_interest_strategy()
