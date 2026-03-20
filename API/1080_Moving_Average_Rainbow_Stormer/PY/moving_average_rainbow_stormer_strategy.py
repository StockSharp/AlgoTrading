import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_average_rainbow_stormer_strategy(Strategy):
    def __init__(self):
        super(moving_average_rainbow_stormer_strategy, self).__init__()
        self._target_factor = self.Param("TargetFactor", 2.0)
        self._cooldown_bars = self.Param("CooldownBars", 40) \
            .SetGreaterThanZero()
        self._min_trend_spread_percent = self.Param("MinTrendSpreadPercent", 0.05) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10)))
        self._entry_price = 0.0
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(moving_average_rainbow_stormer_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted(self, time):
        super(moving_average_rainbow_stormer_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._bar_index = 0
        self._last_signal_bar = -1000000
        self._ma3 = ExponentialMovingAverage()
        self._ma3.Length = 3
        self._ma8 = ExponentialMovingAverage()
        self._ma8.Length = 8
        self._ma20 = ExponentialMovingAverage()
        self._ma20.Length = 20
        self._ma50 = ExponentialMovingAverage()
        self._ma50.Length = 50
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma3, self._ma8, self._ma20, self._ma50, self.OnProcess).Start()

    def OnProcess(self, candle, ma3, ma8, ma20, ma50):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        close = float(candle.ClosePrice)
        m3 = float(ma3)
        m8 = float(ma8)
        m20 = float(ma20)
        m50 = float(ma50)
        bullish_alignment = m3 > m8 and m8 > m20 and m20 > m50
        bearish_alignment = m3 < m8 and m8 < m20 and m20 < m50
        trend_spread_percent = abs(m3 - m50) / close * 100.0 if close != 0.0 else 0.0
        cd = self._cooldown_bars.Value
        can_signal = self._bar_index - self._last_signal_bar >= cd
        min_spread = float(self._min_trend_spread_percent.Value)
        bullish_signal = bullish_alignment and trend_spread_percent >= min_spread
        bearish_signal = bearish_alignment and trend_spread_percent >= min_spread
        tf = float(self._target_factor.Value)
        if can_signal and bullish_signal and close > m3 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._last_signal_bar = self._bar_index
        elif can_signal and bearish_signal and close < m3 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._last_signal_bar = self._bar_index
        if can_signal and self.Position > 0 and self._entry_price > 0.0:
            risk = self._entry_price - m20
            if risk > 0.0:
                target = self._entry_price + risk * tf
                if close >= target or close < m20:
                    self.SellMarket()
                    self._last_signal_bar = self._bar_index
            elif close < m8:
                self.SellMarket()
                self._last_signal_bar = self._bar_index
        elif can_signal and self.Position < 0 and self._entry_price > 0.0:
            risk = m20 - self._entry_price
            if risk > 0.0:
                target = self._entry_price - risk * tf
                if close <= target or close > m20:
                    self.BuyMarket()
                    self._last_signal_bar = self._bar_index
            elif close > m8:
                self.BuyMarket()
                self._last_signal_bar = self._bar_index

    def CreateClone(self):
        return moving_average_rainbow_stormer_strategy()
