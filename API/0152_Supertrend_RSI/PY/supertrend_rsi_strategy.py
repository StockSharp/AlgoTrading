import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class supertrend_rsi_strategy(Strategy):
    """
    Strategy combining manual Supertrend with RSI.
    """

    def __init__(self):
        super(supertrend_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetRange(5, 30) \
            .SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Supertrend")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("RSI Period", "Period for RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._highs = []
        self._lows = []
        self._closes = []
        self._prev_supertrend = 0.0
        self._prev_up_trend = True
        self._st_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(supertrend_rsi_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []
        self._closes = []
        self._prev_supertrend = 0.0
        self._prev_up_trend = True
        self._st_initialized = False
        self._cooldown = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

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
        self._closes.append(close)

        period = self._atr_period.Value

        if len(self._closes) < period + 1:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Manual ATR
        sum_tr = 0.0
        count = len(self._highs)
        for i in range(count - period, count):
            h = self._highs[i]
            l = self._lows[i]
            prev_c = self._closes[i - 1]
            tr = max(h - l, max(abs(h - prev_c), abs(l - prev_c)))
            sum_tr += tr
        atr = sum_tr / period

        # Manual Supertrend
        mult = float(self._multiplier.Value)
        mid_price = (high + low) / 2.0
        upper_band = mid_price + mult * atr
        lower_band = mid_price - mult * atr

        if not self._st_initialized:
            up_trend = close > mid_price
            supertrend = lower_band if up_trend else upper_band
            self._st_initialized = True
        else:
            if self._prev_up_trend:
                if lower_band < self._prev_supertrend:
                    lower_band = self._prev_supertrend
                up_trend = close >= lower_band
                supertrend = lower_band if up_trend else upper_band
            else:
                if upper_band > self._prev_supertrend:
                    upper_band = self._prev_supertrend
                up_trend = close > upper_band
                supertrend = lower_band if up_trend else upper_band

        self._prev_supertrend = supertrend
        self._prev_up_trend = up_trend

        # Trim lists
        if len(self._highs) > period * 3:
            trim = len(self._highs) - period * 2
            self._highs = self._highs[trim:]
            self._lows = self._lows[trim:]
            self._closes = self._closes[trim:]

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if self.Position != 0:
            return

        cd = self._cooldown_bars.Value

        # Buy: uptrend + RSI below midpoint
        if up_trend and rv < 50:
            self.BuyMarket()
            self._cooldown = cd
        # Sell: downtrend + RSI above midpoint
        elif not up_trend and rv > 50:
            self.SellMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(supertrend_rsi_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._prev_supertrend = 0.0
        self._prev_up_trend = True
        self._st_initialized = False
        self._cooldown = 0

    def CreateClone(self):
        return supertrend_rsi_strategy()
