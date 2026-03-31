import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sma_trend_filter_strategy(Strategy):
    def __init__(self):
        super(sma_trend_filter_strategy, self).__init__()
        self._open_level = self.Param("OpenLevel", 1) \
            .SetDisplay("Open Level", "Signal threshold to open position", "Trading")
        self._close_level = self.Param("CloseLevel", 0) \
            .SetDisplay("Close Level", "Signal threshold to close position", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Minimum number of primary timeframe bars between orders", "Trading")
        self._candle_type1 = self.Param("CandleType1", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type 1", "Primary timeframe", "General")
        self._candle_type2 = self.Param("CandleType2", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type 2", "Secondary timeframe", "General")
        self._candle_type3 = self.Param("CandleType3", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type 3", "Tertiary timeframe", "General")

        self._periods = [5, 8, 13, 21, 34]
        self._smas = [[None] * 5 for _ in range(3)]
        self._previous = [[0.0] * 5 for _ in range(3)]
        self._uitog = [0.0] * 3
        self._ditog = [0.0] * 3
        self._is_ready = [False] * 3
        self._signal = 0
        self._bars_since_trade = 0

        for i in range(3):
            for j in range(5):
                sma = SimpleMovingAverage()
                sma.Length = self._periods[j]
                self._smas[i][j] = sma

    @property
    def open_level(self):
        return self._open_level.Value

    @property
    def close_level(self):
        return self._close_level.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type1(self):
        return self._candle_type1.Value

    @property
    def candle_type2(self):
        return self._candle_type2.Value

    @property
    def candle_type3(self):
        return self._candle_type3.Value

    def OnReseted(self):
        super(sma_trend_filter_strategy, self).OnReseted()
        self._signal = 0
        self._bars_since_trade = 0
        for i in range(3):
            self._uitog[i] = 0.0
            self._ditog[i] = 0.0
            self._is_ready[i] = False
            for j in range(5):
                self._previous[i][j] = 0.0
                self._smas[i][j].Reset()

    def OnStarted2(self, time):
        super(sma_trend_filter_strategy, self).OnStarted2(time)
        sub1 = self.SubscribeCandles(self.candle_type1)
        sub1.Bind(self._smas[0][0], self._smas[0][1], self._smas[0][2], self._smas[0][3], self._smas[0][4], self.process_tf1).Start()
        sub2 = self.SubscribeCandles(self.candle_type2)
        sub2.Bind(self._smas[1][0], self._smas[1][1], self._smas[1][2], self._smas[1][3], self._smas[1][4], self.process_tf2).Start()
        sub3 = self.SubscribeCandles(self.candle_type3)
        sub3.Bind(self._smas[2][0], self._smas[2][1], self._smas[2][2], self._smas[2][3], self._smas[2][4], self.process_tf3).Start()

    def process_tf1(self, candle, sma5, sma8, sma13, sma21, sma34):
        self._process_tf(0, candle, [sma5, sma8, sma13, sma21, sma34])

    def process_tf2(self, candle, sma5, sma8, sma13, sma21, sma34):
        self._process_tf(1, candle, [sma5, sma8, sma13, sma21, sma34])

    def process_tf3(self, candle, sma5, sma8, sma13, sma21, sma34):
        self._process_tf(2, candle, [sma5, sma8, sma13, sma21, sma34])

    def _process_tf(self, index, candle, values):
        if candle.State != CandleStates.Finished:
            return

        up = 0
        down = 0
        is_ready = True

        for i in range(5):
            val = float(values[i])
            if val == 0.0:
                return
            prev = self._previous[index][i]
            if prev == 0.0:
                self._previous[index][i] = val
                is_ready = False
                continue
            if val > prev:
                up += 1
            elif val < prev:
                down += 1
            self._previous[index][i] = val

        if not is_ready:
            return

        self._is_ready[index] = True
        self._uitog[index] = up / 5.0 * 100.0
        self._ditog[index] = down / 5.0 * 100.0

        if index == 0:
            self._bars_since_trade += 1
            self._evaluate_signal()

    def _evaluate_signal(self):
        if not self._is_ready[0] or not self._is_ready[1] or not self._is_ready[2]:
            return

        self._signal = 0

        if self._uitog[0] >= 100.0 and self._uitog[1] >= 100.0 and self._uitog[2] >= 100.0:
            self._signal = 2
        elif self._ditog[0] >= 100.0 and self._ditog[1] >= 100.0 and self._ditog[2] >= 100.0:
            self._signal = -2
        elif self._uitog[0] >= 80.0 and self._uitog[1] >= 80.0 and self._uitog[2] >= 80.0:
            self._signal = 1
        elif self._ditog[0] >= 80.0 and self._ditog[1] >= 80.0 and self._ditog[2] >= 80.0:
            self._signal = -1

        open_buy = self._signal > self.open_level
        open_sell = self._signal < -self.open_level
        close_buy = self._signal <= -self.close_level
        close_sell = self._signal >= self.close_level

        if self.Position > 0 and close_buy:
            self.SellMarket()
            self._bars_since_trade = 0

        if self.Position < 0 and close_sell:
            self.BuyMarket()
            self._bars_since_trade = 0

        if self._bars_since_trade < self.cooldown_bars:
            return

        if open_buy and self.Position <= 0 and not close_buy:
            self.BuyMarket()
            self._bars_since_trade = 0

        if open_sell and self.Position >= 0 and not close_sell:
            self.SellMarket()
            self._bars_since_trade = 0

    def CreateClone(self):
        return sma_trend_filter_strategy()
