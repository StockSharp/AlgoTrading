import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class donchian_rsi_strategy(Strategy):
    """
    Strategy combining manual Donchian Channels with RSI.
    """

    def __init__(self):
        super(donchian_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Donchian Period", "Period for Donchian Channels", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("RSI Period", "Period for RSI", "Indicators")
        self._rsi_overbought_level = self.Param("RsiOverboughtLevel", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Trading Levels")
        self._rsi_oversold_level = self.Param("RsiOversoldLevel", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Trading Levels")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._highs = []
        self._lows = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(donchian_rsi_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        rv = float(rsi_value)

        self._highs.append(high)
        self._lows.append(low)

        period = self._donchian_period.Value

        if len(self._highs) < period + 1:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Previous Donchian channel (excluding current bar)
        count = len(self._highs)
        prev_upper = max(self._highs[count - period - 1:count - 1])
        prev_lower = min(self._lows[count - period - 1:count - 1])
        middle_band = (prev_upper + prev_lower) / 2.0

        # Trim lists
        if len(self._highs) > period * 3:
            trim = len(self._highs) - period * 2
            self._highs = self._highs[trim:]
            self._lows = self._lows[trim:]

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        ob = self._rsi_overbought_level.Value
        os_level = self._rsi_oversold_level.Value

        # Buy: upper breakout + RSI not overbought
        if close > prev_upper and rv < ob and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < prev_lower and rv > os_level and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price below middle
        if self.Position > 0 and close < middle_band:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > middle_band:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(donchian_rsi_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._cooldown = 0

    def CreateClone(self):
        return donchian_rsi_strategy()
