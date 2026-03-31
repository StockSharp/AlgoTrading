import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class adx_stochastic_strategy(Strategy):
    """
    Strategy combining ADX for trend strength and manual Stochastic %K for entry timing.
    Enters when ADX shows strong trend and Stochastic is oversold/overbought.
    """

    def __init__(self):
        super(adx_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX level for strong trend", "Indicators")

        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators")

        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._adx_value = 0.0
        self._cooldown = 0
        self._highs = []
        self._lows = []
        self._STOCH_PERIOD = 14

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def adx_threshold(self):
        return self._adx_threshold.Value

    @property
    def stoch_oversold(self):
        return self._stoch_oversold.Value

    @property
    def stoch_overbought(self):
        return self._stoch_overbought.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted2(self, time):
        super(adx_stochastic_strategy, self).OnStarted2(time)

        self._adx_value = 0.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        # Extract ADX value
        if hasattr(adx_value, 'MovingAverage') and adx_value.MovingAverage is not None:
            self._adx_value = float(adx_value.MovingAverage)

        if self._adx_value == 0:
            return

        # Track highs/lows for manual stochastic
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        max_buf = self._STOCH_PERIOD * 2
        if len(self._highs) > max_buf:
            self._highs = self._highs[-max_buf:]
            self._lows = self._lows[-max_buf:]

        if len(self._highs) < self._STOCH_PERIOD:
            return

        # Manual Stochastic %K
        start = len(self._highs) - self._STOCH_PERIOD
        highest_high = max(self._highs[start:])
        lowest_low = min(self._lows[start:])

        diff = highest_high - lowest_low
        if diff == 0:
            return

        close = float(candle.ClosePrice)
        stoch_k = 100.0 * (close - lowest_low) / diff

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        strong_trend = self._adx_value > self.adx_threshold

        # Long: strong trend + stochastic oversold
        if strong_trend and stoch_k < self.stoch_oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Short: strong trend + stochastic overbought
        elif strong_trend and stoch_k > self.stoch_overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit when trend weakens
        if not strong_trend and self.Position > 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif not strong_trend and self.Position < 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(adx_stochastic_strategy, self).OnReseted()
        self._adx_value = 0.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

    def CreateClone(self):
        return adx_stochastic_strategy()
