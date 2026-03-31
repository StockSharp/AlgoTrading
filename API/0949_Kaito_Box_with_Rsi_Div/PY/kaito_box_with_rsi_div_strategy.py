import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kaito_box_with_rsi_div_strategy(Strategy):
    def __init__(self):
        super(kaito_box_with_rsi_div_strategy, self).__init__()
        self._box_length = self.Param("BoxLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Box Length", "Length of the box range", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "Length of RSI", "General")
        self._ma_period = self.Param("MaPeriod", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Trend moving average period", "Trend")
        self._hold_bars = self.Param("HoldBars", 240) \
            .SetGreaterThanZero() \
            .SetDisplay("Hold Bars", "Bars to hold position before forced exit", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 240) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(3))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._bars_in_position = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kaito_box_with_rsi_div_strategy, self).OnReseted()
        self._bars_in_position = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(kaito_box_with_rsi_div_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self._box_length.Value
        lowest = Lowest()
        lowest.Length = self._box_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        self._bars_since_signal = self._cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, rsi, ma, self.OnProcess).Start()

    def OnProcess(self, candle, highest_val, lowest_val, rsi_val, ma_val):
        if candle.State != CandleStates.Finished:
            return
        h_val = float(highest_val)
        l_val = float(lowest_val)
        rsi_v = float(rsi_val)
        ma_v = float(ma_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        hold = self._hold_bars.Value
        cd = self._cooldown_bars.Value
        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= hold:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_position = 0
                self._bars_since_signal = 0
            return
        self._bars_in_position = 0
        self._bars_since_signal += 1
        if self._bars_since_signal < cd:
            return
        long_signal = high >= h_val and rsi_v > 55 and close > ma_v
        short_signal = low <= l_val and rsi_v < 45 and close < ma_v
        if long_signal:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif short_signal:
            self.SellMarket()
            self._bars_since_signal = 0

    def CreateClone(self):
        return kaito_box_with_rsi_div_strategy()
