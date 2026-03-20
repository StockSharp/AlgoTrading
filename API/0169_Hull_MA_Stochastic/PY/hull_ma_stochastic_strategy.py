import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class hull_ma_stochastic_strategy(Strategy):

    def __init__(self):
        super(hull_ma_stochastic_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators")
        self._stoch_k = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
        self._stoch_d = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 90) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._hma = None
        self._stochastic = None
        self._atr = None
        self._prev_hma_value = 0.0
        self._cooldown = 0

    @property
    def HmaPeriod(self):
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def StochPeriod(self):
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochK(self):
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochD(self):
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        super(hull_ma_stochastic_strategy, self).OnStarted(time)

        self._prev_hma_value = 0.0
        self._cooldown = 0

        self._hma = HullMovingAverage()
        self._hma.Length = self.HmaPeriod

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochK
        self._stochastic.D.Length = self.StochD

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        self.SubscribeCandles(self.CandleType) \
            .BindEx(self._hma, self._stochastic, self._atr, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, hma_value, stoch_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hma_f = float(hma_value.ToDecimal())
        stoch_k = stoch_value.K
        if stoch_k is None:
            return
        stoch_k_f = float(stoch_k)

        if self._prev_hma_value == 0:
            self._prev_hma_value = hma_f
            return

        hma_increasing = hma_f > self._prev_hma_value
        hma_decreasing = hma_f < self._prev_hma_value
        cooldown_bars = int(self.CooldownBars)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_hma_value = hma_f
            return

        if hma_increasing and stoch_k_f > 50 and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cooldown_bars
        elif hma_decreasing and stoch_k_f < 50 and self.Position == 0:
            self.SellMarket()
            self._cooldown = cooldown_bars
        elif self.Position > 0 and hma_decreasing:
            self.SellMarket()
            self._cooldown = cooldown_bars
        elif self.Position < 0 and hma_increasing:
            self.BuyMarket()
            self._cooldown = cooldown_bars

        self._prev_hma_value = hma_f

    def OnReseted(self):
        super(hull_ma_stochastic_strategy, self).OnReseted()
        self._hma = None
        self._stochastic = None
        self._atr = None
        self._prev_hma_value = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return hull_ma_stochastic_strategy()
