import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class arpit_bollinger_band_strategy(Strategy):
    """
    Bollinger Band reversal strategy.
    Buys when price crosses below lower band then returns above.
    Sells when price crosses above upper band then returns below.
    """

    def __init__(self):
        super(arpit_bollinger_band_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Bollinger Bands length", "Bollinger")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "StdDev multiplier", "Bollinger")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def BollingerLength(self): return self._bollinger_length.Value
    @BollingerLength.setter
    def BollingerLength(self, v): self._bollinger_length.Value = v
    @property
    def BollingerMultiplier(self): return self._bollinger_multiplier.Value
    @BollingerMultiplier.setter
    def BollingerMultiplier(self, v): self._bollinger_multiplier.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v

    def OnReseted(self):
        super(arpit_bollinger_band_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(arpit_bollinger_band_strategy, self).OnStarted(time)

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerLength
        bollinger.Width = self.BollingerMultiplier

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        if bb_value.IsEmpty:
            return

        upper = get_bb_upper(bb_value)
        lower = get_bb_lower(bb_value)

        if upper == 0 or lower == 0:
            return

        close = float(candle.ClosePrice)
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        cross_up_from_below = self._prev_close <= self._prev_lower and self._prev_lower > 0 and close > lower
        cross_down_from_above = self._prev_close >= self._prev_upper and self._prev_upper > 0 and close < upper

        if cross_up_from_below and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down_from_above and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return arpit_bollinger_band_strategy()
