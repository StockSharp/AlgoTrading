import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class arsi_vwap_atr_strategy(Strategy):
    """
    Adaptive RSI strategy with dynamic OB/OS levels.
    Uses RSI crossover of overbought/oversold thresholds with EMA trend filter.
    """

    def __init__(self):
        super(arsi_vwap_atr_strategy, self).__init__()

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation period", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._ob_level = self.Param("ObLevel", 55.0) \
            .SetDisplay("OB Level", "Overbought RSI level", "Indicators")
        self._os_level = self.Param("OsLevel", 45.0) \
            .SetDisplay("OS Level", "Oversold RSI level", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 300) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def RsiLength(self): return self._rsi_length.Value
    @RsiLength.setter
    def RsiLength(self, v): self._rsi_length.Value = v
    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def ObLevel(self): return self._ob_level.Value
    @ObLevel.setter
    def ObLevel(self, v): self._ob_level.Value = v
    @property
    def OsLevel(self): return self._os_level.Value
    @OsLevel.setter
    def OsLevel(self, v): self._os_level.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(arsi_vwap_atr_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(arsi_vwap_atr_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        long_signal = self._prev_rsi > 0 and self._prev_rsi < self.OsLevel and rsi_value >= self.OsLevel
        short_signal = self._prev_rsi > 0 and self._prev_rsi > self.ObLevel and rsi_value <= self.ObLevel

        if long_signal and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif short_signal and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_rsi = rsi_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return arsi_vwap_atr_strategy()
