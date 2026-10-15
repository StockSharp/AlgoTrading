import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class larry_connors_3_day_high_low_strategy(Strategy):
    def __init__(self):
        super(larry_connors_3_day_high_low_strategy, self).__init__()
        self._long_ma_length = self.Param("LongMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Long MA Length", "Period of the long moving average", "General")
        self._short_ma_length = self.Param("ShortMaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Short MA Length", "Period of the short moving average", "General")
        self._max_entries = self.Param("MaxEntries", 35) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between orders", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bar_count = 0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(larry_connors_3_day_high_low_strategy, self).OnReseted()
        self._bar_count = 0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(larry_connors_3_day_high_low_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        self._long_sma = SimpleMovingAverage()
        self._long_sma.Length = self._long_ma_length.Value
        self._short_sma = SimpleMovingAverage()
        self._short_sma.Length = self._short_ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._long_sma, self._short_sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._long_sma)
            self.DrawIndicator(area, self._short_sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, long_ma_val, short_ma_val):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_signal += 1

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        lma = float(long_ma_val)
        sma = float(short_ma_val)

        if self._long_sma.IsFormed and self._short_sma.IsFormed and self._bar_count >= 3:
            # Exit: close long when price crosses above short MA
            if close > sma and self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self._bars_since_signal = 0
            # Entry: buy after 3 consecutive lower highs/lows, close below short MA, above long MA
            elif self._bars_since_signal >= self._cooldown_bars.Value and self.Position <= 0 and self._entries_executed < self._max_entries.Value:
                above_long_ma = close > lma
                below_short_ma = close < sma
                lower_highs_lows_3 = self._high2 < self._high3 and self._low2 < self._low3
                lower_highs_lows_2 = self._high1 < self._high2 and self._low1 < self._low2
                lower_highs_lows_1 = high < self._high1 and low < self._low1

                if above_long_ma and below_short_ma and lower_highs_lows_3 and lower_highs_lows_2 and lower_highs_lows_1:
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self._entries_executed += 1
                    self._bars_since_signal = 0

        self._bar_count += 1
        self._high3 = self._high2
        self._high2 = self._high1
        self._high1 = high
        self._low3 = self._low2
        self._low2 = self._low1
        self._low1 = low

    def CreateClone(self):
        return larry_connors_3_day_high_low_strategy()
