import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mtf_oscillator_framework_strategy(Strategy):
    """
    MTF Oscillator Framework: RSI zone crossover with exits at neutral levels.
    """

    def __init__(self):
        super(mtf_oscillator_framework_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._overbought = self.Param("Overbought", 68.0).SetDisplay("OB", "Overbought", "Signals")
        self._oversold = self.Param("Oversold", 32.0).SetDisplay("OS", "Oversold", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 6).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_rsi = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mtf_oscillator_framework_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted2(self, time):
        super(mtf_oscillator_framework_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        rsi = float(rsi_val)
        self._bar_index += 1
        if not self._has_prev:
            self._prev_rsi = rsi
            self._has_prev = True
            return
        ob = float(self._overbought.Value)
        os_val = float(self._oversold.Value)
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars.Value
        long_signal = self._prev_rsi <= os_val and rsi > os_val
        short_signal = self._prev_rsi >= ob and rsi < ob
        if can_signal and long_signal and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_bar = self._bar_index
        elif can_signal and short_signal and self.Position >= 0:
            self.SellMarket()
            self._last_signal_bar = self._bar_index
        if self.Position > 0 and rsi >= 60.0:
            self.SellMarket()
        elif self.Position < 0 and rsi <= 40.0:
            self.BuyMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return mtf_oscillator_framework_strategy()
