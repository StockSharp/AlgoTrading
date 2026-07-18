import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ma_adx_strategy(Strategy):
    """
    Strategy combining MA trend filter with manual ADX-like trend strength.
    Enters when price crosses MA with strong directional movement.
    """

    def __init__(self):
        super(ma_adx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("MA Period", "Period of the Moving Average", "Indicators")

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX level for strong trend", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def adx_threshold(self):
        return self._adx_threshold.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted2(self, time):
        super(ma_adx_strategy, self).OnStarted2(time)

        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        mv = float(ma_value)

        self._highs.append(high)
        self._lows.append(low)
        self._closes.append(close)

        adx_p = self.adx_period

        # Need at least adxPeriod+2 bars
        if len(self._closes) < adx_p + 2:
            if self._cooldown > 0:
                self._cooldown -= 1
            return

        # Manual ADX-like trend strength calculation
        sum_tr = 0.0
        sum_dm_plus = 0.0
        sum_dm_minus = 0.0

        count = len(self._highs)
        start = count - adx_p

        for i in range(start, count):
            h = self._highs[i]
            l = self._lows[i]
            prev_c = self._closes[i - 1]
            prev_h = self._highs[i - 1]
            prev_l = self._lows[i - 1]

            tr = max(h - l, max(abs(h - prev_c), abs(l - prev_c)))
            sum_tr += tr

            up_move = h - prev_h
            down_move = prev_l - l

            if up_move > down_move and up_move > 0:
                sum_dm_plus += up_move

            if down_move > up_move and down_move > 0:
                sum_dm_minus += down_move

        trend_strength = 0.0
        if sum_tr > 0:
            di_plus = 100.0 * sum_dm_plus / sum_tr
            di_minus = 100.0 * sum_dm_minus / sum_tr
            di_sum = di_plus + di_minus
            trend_strength = 100.0 * abs(di_plus - di_minus) / di_sum if di_sum > 0 else 0.0

        # Keep lists manageable
        if len(self._highs) > adx_p * 3:
            trim = len(self._highs) - adx_p * 2
            self._highs = self._highs[trim:]
            self._lows = self._lows[trim:]
            self._closes = self._closes[trim:]

        strong_trend = trend_strength > self.adx_threshold

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long: price above MA + strong trend
        if close > mv and strong_trend and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Short: price below MA + strong trend
        elif close < mv and strong_trend and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: price crosses below MA
        if self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: price crosses above MA
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(ma_adx_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

    def CreateClone(self):
        return ma_adx_strategy()
