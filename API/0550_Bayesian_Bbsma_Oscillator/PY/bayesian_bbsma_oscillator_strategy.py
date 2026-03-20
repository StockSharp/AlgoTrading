import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bayesian_bbsma_oscillator_strategy(Strategy):
    def __init__(self):
        super(bayesian_bbsma_oscillator_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_period = self.Param("BbSmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB SMA Period", "Bollinger Bands SMA period", "Bollinger Bands")
        self._bb_width = self.Param("BbStdDevMult", 2.5) \
            .SetDisplay("BB StdDev Mult", "Bollinger Bands std dev multiplier", "Bollinger Bands")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Simple moving average period", "General")
        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_close_above_upper = 0.0
        self._prev_close_below_upper = 0.0
        self._prev_close_above_basis = 0.0
        self._prev_close_below_basis = 0.0
        self._prev_close_above_sma = 0.0
        self._prev_close_below_sma = 0.0
        self._count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(bayesian_bbsma_oscillator_strategy, self).OnReseted()
        self._prev_close_above_upper = 0.0
        self._prev_close_below_upper = 0.0
        self._prev_close_above_basis = 0.0
        self._prev_close_below_basis = 0.0
        self._prev_close_above_sma = 0.0
        self._prev_close_below_sma = 0.0
        self._count = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bayesian_bbsma_oscillator_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_width.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, sma_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bb_value
        upper = bb.UpBand
        lower = bb.LowBand
        basis = bb.MovingAverage
        if upper is None or lower is None or basis is None:
            return
        upper_v = float(upper)
        basis_v = float(basis)
        sma_v = float(sma_value.GetValue[float]())
        close = float(candle.ClosePrice)
        alpha = 0.1
        self._count += 1
        if close > upper_v:
            self._prev_close_above_upper = self._prev_close_above_upper * (1 - alpha) + alpha
        else:
            self._prev_close_above_upper = self._prev_close_above_upper * (1 - alpha)
        if close < upper_v:
            self._prev_close_below_upper = self._prev_close_below_upper * (1 - alpha) + alpha
        else:
            self._prev_close_below_upper = self._prev_close_below_upper * (1 - alpha)
        if close > basis_v:
            self._prev_close_above_basis = self._prev_close_above_basis * (1 - alpha) + alpha
        else:
            self._prev_close_above_basis = self._prev_close_above_basis * (1 - alpha)
        if close < basis_v:
            self._prev_close_below_basis = self._prev_close_below_basis * (1 - alpha) + alpha
        else:
            self._prev_close_below_basis = self._prev_close_below_basis * (1 - alpha)
        if close > sma_v:
            self._prev_close_above_sma = self._prev_close_above_sma * (1 - alpha) + alpha
        else:
            self._prev_close_above_sma = self._prev_close_above_sma * (1 - alpha)
        if close < sma_v:
            self._prev_close_below_sma = self._prev_close_below_sma * (1 - alpha) + alpha
        else:
            self._prev_close_below_sma = self._prev_close_below_sma * (1 - alpha)
        if self._count < 20:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        s_up = self._prev_close_above_upper + self._prev_close_below_upper
        s_ba = self._prev_close_above_basis + self._prev_close_below_basis
        s_sm = self._prev_close_above_sma + self._prev_close_below_sma
        if s_up == 0 or s_ba == 0 or s_sm == 0:
            return
        p_up_u = self._prev_close_above_upper / s_up
        p_up_b = self._prev_close_above_basis / s_ba
        p_up_s = self._prev_close_above_sma / s_sm
        p_dn_u = self._prev_close_below_upper / s_up
        p_dn_b = self._prev_close_below_basis / s_ba
        p_dn_s = self._prev_close_below_sma / s_sm
        num_up = p_dn_u * p_dn_b * p_dn_s
        den_up = num_up + (1 - p_dn_u) * (1 - p_dn_b) * (1 - p_dn_s)
        sigma_up = num_up / den_up if den_up != 0 else 0
        if sigma_up > 0.7 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif sigma_up < 0.3 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return bayesian_bbsma_oscillator_strategy()
