import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class bollinger_adx_strategy(Strategy):
    """
    Strategy combining Bollinger Bands with manual ADX trend strength.
    """

    def __init__(self):
        super(bollinger_adx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetRange(10, 30) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(7, 21) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
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

    def OnStarted2(self, time):
        super(bollinger_adx_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

        bb = BollingerBands()
        bb.Length = self._bollinger_period.Value
        bb.Width = self._bollinger_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._highs.append(high)
        self._lows.append(low)
        self._closes.append(close)

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)

        adx_p = self._adx_period.Value

        # Manual ADX trend strength
        trend_strength = 0.0
        if len(self._closes) >= adx_p + 2:
            sum_tr = 0.0
            sum_dm_plus = 0.0
            sum_dm_minus = 0.0
            count = len(self._highs)
            for i in range(count - adx_p, count):
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

            if sum_tr > 0:
                di_plus = 100.0 * sum_dm_plus / sum_tr
                di_minus = 100.0 * sum_dm_minus / sum_tr
                di_sum = di_plus + di_minus
                trend_strength = 100.0 * abs(di_plus - di_minus) / di_sum if di_sum > 0 else 0.0

        # Trim lists
        max_keep = adx_p * 3
        if len(self._highs) > max_keep:
            trim = len(self._highs) - adx_p * 2
            self._highs = self._highs[trim:]
            self._lows = self._lows[trim:]
            self._closes = self._closes[trim:]

        strong_trend = trend_strength > self._adx_threshold.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value

        # Buy: price above upper band + strong trend
        if close > upper_band and strong_trend and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < lower_band and strong_trend and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price returns to middle band
        if self.Position > 0 and close < middle_band:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > middle_band:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(bollinger_adx_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

    def CreateClone(self):
        return bollinger_adx_strategy()
