import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ArnaudLegouxMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class alma_optimized_strategy(Strategy):
    """
    ALMA based strategy with EMA crossover.
    Goes long when ALMA crosses above slow EMA, short when crosses below.
    """

    def __init__(self):
        super(alma_optimized_strategy, self).__init__()

        self._alma_length = self.Param("AlmaLength", 9) \
            .SetDisplay("ALMA Length", "ALMA period", "Indicator")

        self._ema_length = self.Param("EmaLength", 26) \
            .SetDisplay("EMA Length", "Slow EMA period", "Indicator")

        self._cooldown_bars = self.Param("CooldownBars", 40) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Trading")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._bar_index = 0
        self._last_trade_bar = 0
        self._prev_alma = 0.0
        self._prev_ema = 0.0

    @property
    def AlmaLength(self): return self._alma_length.Value
    @AlmaLength.setter
    def AlmaLength(self, v): self._alma_length.Value = v
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
        super(alma_optimized_strategy, self).OnReseted()
        self._bar_index = 0
        self._last_trade_bar = 0
        self._prev_alma = 0.0
        self._prev_ema = 0.0

    def OnStarted2(self, time):
        super(alma_optimized_strategy, self).OnStarted2(time)

        alma = ArnaudLegouxMovingAverage()
        alma.Length = self.AlmaLength
        alma.Offset = 0.65
        alma.Sigma = 6
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(alma, ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, alma)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, alma_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1

        if self._prev_alma == 0 or self._prev_ema == 0:
            self._prev_alma = alma_value
            self._prev_ema = ema_value
            return

        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        cross_over = self._prev_alma <= self._prev_ema and alma_value > ema_value
        cross_under = self._prev_alma >= self._prev_ema and alma_value < ema_value

        if cross_over and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_under and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_alma = alma_value
        self._prev_ema = ema_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alma_optimized_strategy()
