import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import (
    BearPower, BullPower, Momentum,
    MovingAverageConvergenceDivergenceSignal,
    StochasticOscillator, ForceIndex,
)
from StockSharp.Algo.Strategies import Strategy


class svm_trader_strategy(Strategy):

    def __init__(self):
        super(svm_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._take_profit = self.Param("TakeProfit", 1400.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 900.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._risk_exposure = self.Param("RiskExposure", 1.0) \
            .SetDisplay("Risk Exposure", "Max cumulative position", "Risk")
        self._buy_threshold = self.Param("BuyThreshold", 4) \
            .SetDisplay("Buy Threshold", "Score required for a long signal", "Signal")
        self._sell_threshold = self.Param("SellThreshold", 1) \
            .SetDisplay("Sell Threshold", "Score required for a short signal", "Signal")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")

        self._previous_score = 0
        self._has_previous_score = False
        self._bars_since_trade = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def RiskExposure(self):
        return self._risk_exposure.Value

    @RiskExposure.setter
    def RiskExposure(self, value):
        self._risk_exposure.Value = value

    @property
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @BuyThreshold.setter
    def BuyThreshold(self, value):
        self._buy_threshold.Value = value

    @property
    def SellThreshold(self):
        return self._sell_threshold.Value

    @SellThreshold.setter
    def SellThreshold(self, value):
        self._sell_threshold.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnStarted2(self, time):
        super(svm_trader_strategy, self).OnStarted2(time)

        bears = BearPower()
        bears.Length = 13
        bulls = BullPower()
        bulls.Length = 13
        momentum = Momentum()
        momentum.Length = 13
        macd = MovingAverageConvergenceDivergenceSignal()
        stochastic = StochasticOscillator()
        force = ForceIndex()
        force.Length = 13

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(bears, bulls, momentum, macd, stochastic, force, self.ProcessIndicators) \
            .Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, bears_value, bulls_value, momentum_value,
                          macd_value, stochastic_value, force_value):
        if candle.State != CandleStates.Finished:
            return
        if (not bears_value.IsFinal or not bulls_value.IsFinal or not momentum_value.IsFinal
                or not macd_value.IsFinal or not stochastic_value.IsFinal or not force_value.IsFinal):
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        score = 0
        bears = float(bears_value)
        bulls = float(bulls_value)
        mom = float(momentum_value)
        force_val = float(force_value)

        # MACD typed access
        macd_val = macd_value.Macd
        signal_val = macd_value.Signal

        # Stochastic typed access
        stoch_k = stochastic_value.K
        stoch_d = stochastic_value.D

        if bulls > bears:
            score += 1
        if mom > 100.0:
            score += 1
        if macd_val is not None and signal_val is not None and macd_val > signal_val:
            score += 1
        if stoch_k is not None and stoch_d is not None and stoch_k > stoch_d and stoch_k > 55.0:
            score += 1
        if force_val > 0.0:
            score += 1

        if not self._has_previous_score:
            self._previous_score = score
            self._has_previous_score = True
            return

        long_signal = self._previous_score < self.BuyThreshold and score >= self.BuyThreshold
        short_signal = self._previous_score > self.SellThreshold and score <= self.SellThreshold
        open_volume = abs(self.Position)

        if self._bars_since_trade >= self.CooldownBars and open_volume + float(self.Volume) <= float(self.RiskExposure):
            pos = self.Position
            if long_signal and pos <= 0:
                vol = float(self.Volume) + (-pos if pos < 0 else 0.0)
                self.BuyMarket(vol)
                self._bars_since_trade = 0
            elif short_signal and pos >= 0:
                vol = float(self.Volume) + (pos if pos > 0 else 0.0)
                self.SellMarket(vol)
                self._bars_since_trade = 0

        self._previous_score = score

    def OnReseted(self):
        super(svm_trader_strategy, self).OnReseted()
        self._previous_score = 0
        self._has_previous_score = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return svm_trader_strategy()
