import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ao_divergence_strategy(Strategy):
    """
    AO Divergence strategy.
    Buys when AO crosses above zero with EMA uptrend, sells when AO crosses below zero with downtrend.
    """

    def __init__(self):
        super(ao_divergence_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 40) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 380) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_ao = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(ao_divergence_strategy, self).OnReseted()
        self._prev_ao = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(ao_divergence_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        ao = AwesomeOscillator()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, ao, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, ao_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        ao_cross_up = self._prev_ao <= 0 and ao_value > 0
        ao_cross_down = self._prev_ao >= 0 and ao_value < 0

        if ao_cross_up and float(candle.ClosePrice) > ema_value and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif ao_cross_down and float(candle.ClosePrice) < ema_value and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_ao = ao_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ao_divergence_strategy()
