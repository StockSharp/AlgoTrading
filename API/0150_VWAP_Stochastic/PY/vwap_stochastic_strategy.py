import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class vwap_stochastic_strategy(Strategy):
    """
    Strategy combining VWAP and manual Stochastic %K.
    Buys when price is below VWAP and Stochastic is oversold.
    Sells when price is above VWAP and Stochastic is overbought.
    """

    def __init__(self):
        super(vwap_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Stoch Period", "Lookback period for Stochastic %K", "Indicators")

        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetDisplay("Overbought Level", "Level considered overbought", "Trading Levels")

        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetDisplay("Oversold Level", "Level considered oversold", "Trading Levels")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._highs = []
        self._lows = []
        self._closes = []
        self._volumes = []
        self._typical_price_vol = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stoch_period(self):
        return self._stoch_period.Value

    @property
    def overbought_level(self):
        return self._overbought_level.Value

    @property
    def oversold_level(self):
        return self._oversold_level.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted(self, time):
        super(vwap_stochastic_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._closes = []
        self._volumes = []
        self._typical_price_vol = []
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)
        typical_price = (high + low + close) / 3.0

        self._highs.append(high)
        self._lows.append(low)
        self._closes.append(close)
        self._volumes.append(volume)
        self._typical_price_vol.append(typical_price * volume)

        period = self.stoch_period

        if len(self._closes) < period:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Manual VWAP (cumulative)
        sum_tpv = sum(self._typical_price_vol)
        sum_vol = sum(self._volumes)
        vwap_value = sum_tpv / sum_vol if sum_vol > 0 else close

        # Manual Stochastic %K
        count = len(self._highs)
        start = count - period
        highest_high = max(self._highs[start:count])
        lowest_low = min(self._lows[start:count])

        rng = highest_high - lowest_low
        stoch_k = 100.0 * (close - lowest_low) / rng if rng > 0 else 50.0

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Buy: price below VWAP + Stochastic oversold
        if close < vwap_value and stoch_k < self.oversold_level and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Sell: price above VWAP + Stochastic overbought
        elif close > vwap_value and stoch_k > self.overbought_level and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: price above VWAP or stoch overbought
        if self.Position > 0 and (close > vwap_value or stoch_k > self.overbought_level):
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: price below VWAP or stoch oversold
        elif self.Position < 0 and (close < vwap_value or stoch_k < self.oversold_level):
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(vwap_stochastic_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._volumes = []
        self._typical_price_vol = []
        self._cooldown = 0

    def CreateClone(self):
        return vwap_stochastic_strategy()
