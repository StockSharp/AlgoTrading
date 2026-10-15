import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class mesa_stochastic_multi_length_strategy(Strategy):
    def __init__(self):
        super(mesa_stochastic_multi_length_strategy, self).__init__()
        self._length1 = self.Param("Length1", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Length 1", "Primary stochastic length", "General")
        self._length2 = self.Param("Length2", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Length 2", "Secondary stochastic length", "General")
        self._upper_level = self.Param("UpperLevel", 0.60) \
            .SetDisplay("Upper Level", "Upper signal level", "General")
        self._lower_level = self.Param("LowerLevel", 0.40) \
            .SetDisplay("Lower Level", "Lower signal level", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._prices = []
        self._prev_stoch1 = 0.5
        self._prev_stoch2 = 0.5
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mesa_stochastic_multi_length_strategy, self).OnReseted()
        self._prices = []
        self._prev_stoch1 = 0.5
        self._prev_stoch2 = 0.5
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mesa_stochastic_multi_length_strategy, self).OnStarted2(time)
        self._prices = []
        self._prev_stoch1 = 0.5
        self._prev_stoch2 = 0.5
        self._bars_from_signal = self._signal_cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def _calc_stochastic(self, prices, length):
        count = len(prices)
        if count < length:
            return 0.5
        high = -999999999.0
        low = 999999999.0
        start = count - length
        for i in range(start, count):
            if prices[i] > high:
                high = prices[i]
            if prices[i] < low:
                low = prices[i]
        if high == low:
            return 0.5
        return (prices[count - 1] - low) / (high - low)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        self._prices.append(price)
        max_len = max(self._length1.Value, self._length2.Value)
        if len(self._prices) > max_len + 10:
            self._prices.pop(0)
        if len(self._prices) < max_len:
            return
        stoch1 = self._calc_stochastic(self._prices, self._length1.Value)
        stoch2 = self._calc_stochastic(self._prices, self._length2.Value)
        ul = float(self._upper_level.Value)
        ll = float(self._lower_level.Value)
        up = stoch1 > ul and stoch2 > ul and self._prev_stoch1 <= ul
        down = stoch1 < ll and stoch2 < ll and self._prev_stoch1 >= ll
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and up and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and down and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_stoch1 = stoch1
        self._prev_stoch2 = stoch2

    def CreateClone(self):
        return mesa_stochastic_multi_length_strategy()
