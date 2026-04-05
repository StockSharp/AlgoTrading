import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class bayesian_bbsma_oscillator_strategy(Strategy):
    def __init__(self):
        super(bayesian_bbsma_oscillator_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_sma_period = self.Param("BbSmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB SMA Period", "Bollinger Bands SMA period", "Bollinger Bands")
        self._bb_std_dev_mult = self.Param("BbStdDevMult", 2.5) \
            .SetDisplay("BB StdDev Mult", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._ao_fast = self.Param("AoFast", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("AO Fast", "Fast period for Awesome Oscillator", "Oscillators")
        self._ao_slow = self.Param("AoSlow", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("AO Slow", "Slow period for Awesome Oscillator", "Oscillators")
        self._ac_fast = self.Param("AcFast", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("AC Fast", "Smoothing period for Accelerator Oscillator", "Oscillators")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Simple moving average period", "General")
        self._bayes_period = self.Param("BayesPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Bayes Period", "Lookback period for probability calculation", "Bayesian")
        self._lower_threshold = self.Param("LowerThreshold", 30.0) \
            .SetDisplay("Lower Threshold", "Probability threshold (%)", "Bayesian")
        self._use_bw_confirmation = self.Param("UseBwConfirmation", False) \
            .SetDisplay("Use BW Confirmation", "Require Bill Williams confirmation", "Filters")
        self._jaw_length = self.Param("JawLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Jaw Length", "Alligator jaw SMA length", "Filters")

        self._prev_ao = 0.0
        self._prev_ac = 0.0
        self._prev_sigma_probs_up = 0.0
        self._prev_sigma_probs_down = 0.0
        self._prev_prob_prime = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bayesian_bbsma_oscillator_strategy, self).OnReseted()
        self._prev_ao = 0.0
        self._prev_ac = 0.0
        self._prev_sigma_probs_up = 0.0
        self._prev_sigma_probs_down = 0.0
        self._prev_prob_prime = 0.0

    def OnStarted2(self, time):
        super(bayesian_bbsma_oscillator_strategy, self).OnStarted2(time)

        self._bb = BollingerBands()
        self._bb.Length = self._bb_sma_period.Value
        self._bb.Width = self._bb_std_dev_mult.Value

        self._sma_close = SimpleMovingAverage()
        self._sma_close.Length = self._sma_period.Value

        self._ao_fast_sma = SimpleMovingAverage()
        self._ao_fast_sma.Length = self._ao_fast.Value

        self._ao_slow_sma = SimpleMovingAverage()
        self._ao_slow_sma.Length = self._ao_slow.Value

        self._ac_sma = SimpleMovingAverage()
        self._ac_sma.Length = self._ac_fast.Value

        self._jaw_sma = SimpleMovingAverage()
        self._jaw_sma.Length = self._jaw_length.Value

        self._bb_upper_up_sma = SimpleMovingAverage()
        self._bb_upper_up_sma.Length = self._bayes_period.Value

        self._bb_upper_down_sma = SimpleMovingAverage()
        self._bb_upper_down_sma.Length = self._bayes_period.Value

        self._bb_basis_up_sma = SimpleMovingAverage()
        self._bb_basis_up_sma.Length = self._bayes_period.Value

        self._bb_basis_down_sma = SimpleMovingAverage()
        self._bb_basis_down_sma.Length = self._bayes_period.Value

        self._sma_up_sma = SimpleMovingAverage()
        self._sma_up_sma.Length = self._bayes_period.Value

        self._sma_down_sma = SimpleMovingAverage()
        self._sma_down_sma.Length = self._bayes_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        bb_upper = bb_value.UpBand
        bb_lower = bb_value.LowBand
        bb_basis = bb_value.MovingAverage

        if bb_upper is None or bb_lower is None or bb_basis is None:
            return

        bb_upper = float(bb_upper)
        bb_lower = float(bb_lower)
        bb_basis = float(bb_basis)

        t = candle.ServerTime
        close = float(candle.ClosePrice)
        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0

        sma_val = process_float(self._sma_close, close, t, True)
        ao_fast_val = process_float(self._ao_fast_sma, median, t, True)
        ao_slow_val = process_float(self._ao_slow_sma, median, t, True)
        jaw_val = process_float(self._jaw_sma, close, t, True)

        if not ao_slow_val.IsFormed or not sma_val.IsFormed:
            return

        sma_close = float(sma_val)
        ao_fast_v = float(ao_fast_val)
        ao_slow_v = float(ao_slow_val)
        jaw = float(jaw_val)

        ao = ao_fast_v - ao_slow_v
        ao_sma_value = process_float(self._ac_sma, ao, t, True)
        if not ao_sma_value.IsFormed:
            return

        ac = ao - float(ao_sma_value)

        ac_is_blue = ac > self._prev_ac
        ao_is_green = ao > self._prev_ao

        prob_bb_upper_up = float(process_float(self._bb_upper_up_sma, 1.0 if close > bb_upper else 0.0, t, True))

        prob_bb_upper_down = float(process_float(self._bb_upper_down_sma, 1.0 if close < bb_upper else 0.0, t, True))

        prob_bb_basis_up = float(process_float(self._bb_basis_up_sma, 1.0 if close > bb_basis else 0.0, t, True))

        prob_bb_basis_down = float(process_float(self._bb_basis_down_sma, 1.0 if close < bb_basis else 0.0, t, True))

        prob_sma_up = float(process_float(self._sma_up_sma, 1.0 if close > sma_close else 0.0, t, True))

        prob_sma_down = float(process_float(self._sma_down_sma, 1.0 if close < sma_close else 0.0, t, True))

        if not self._bb_upper_up_sma.IsFormed:
            return

        sum_bb_upper = prob_bb_upper_up + prob_bb_upper_down
        sum_bb_basis = prob_bb_basis_up + prob_bb_basis_down
        sum_sma = prob_sma_up + prob_sma_down

        if sum_bb_upper == 0 or sum_bb_basis == 0 or sum_sma == 0:
            self._prev_ao = ao
            self._prev_ac = ac
            return

        p_up_bb_upper = prob_bb_upper_up / sum_bb_upper
        p_up_bb_basis = prob_bb_basis_up / sum_bb_basis
        p_up_sma = prob_sma_up / sum_sma

        num_down = p_up_bb_upper * p_up_bb_basis * p_up_sma
        den_down = num_down + (1.0 - p_up_bb_upper) * (1.0 - p_up_bb_basis) * (1.0 - p_up_sma)
        sigma_probs_down = num_down / den_down if den_down != 0 else 0.0

        p_down_bb_upper = prob_bb_upper_down / sum_bb_upper
        p_down_bb_basis = prob_bb_basis_down / sum_bb_basis
        p_down_sma = prob_sma_down / sum_sma

        num_up = p_down_bb_upper * p_down_bb_basis * p_down_sma
        den_up = num_up + (1.0 - p_down_bb_upper) * (1.0 - p_down_bb_basis) * (1.0 - p_down_sma)
        sigma_probs_up = num_up / den_up if den_up != 0 else 0.0

        num_prime = sigma_probs_down * sigma_probs_up
        den_prime = num_prime + (1.0 - sigma_probs_down) * (1.0 - sigma_probs_up)
        prob_prime = num_prime / den_prime if den_prime != 0 else 0.0

        threshold = float(self._lower_threshold.Value) / 100.0

        upper_threshold = 1.0 - threshold

        long_signal = (sigma_probs_up > upper_threshold and self._prev_sigma_probs_up <= upper_threshold) or \
                      (prob_prime > upper_threshold and self._prev_prob_prime <= upper_threshold)

        short_signal = (sigma_probs_down > upper_threshold and self._prev_sigma_probs_down <= upper_threshold) or \
                       (prob_prime < threshold and self._prev_prob_prime >= threshold)

        if long_signal and self.Position == 0:
            self.BuyMarket()
        elif short_signal and self.Position == 0:
            self.SellMarket()

        self._prev_ao = ao
        self._prev_ac = ac
        self._prev_sigma_probs_up = sigma_probs_up
        self._prev_sigma_probs_down = sigma_probs_down
        self._prev_prob_prime = prob_prime

    def CreateClone(self):
        return bayesian_bbsma_oscillator_strategy()
