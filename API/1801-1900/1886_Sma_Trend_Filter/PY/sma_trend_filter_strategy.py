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
        self._cooldown_bars = self.Param("CooldownBars", 200) \
            .SetDisplay("Cooldown Bars", "Minimum number of primary timeframe bars between orders", "Trading")
        self._candle_type1 = self.Param("CandleType1", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type 1", "Primary timeframe", "General")
        self._candle_type2 = self.Param("CandleType2", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type 2", "Secondary timeframe", "General")
        self._candle_type3 = self.Param("CandleType3", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type 3", "Tertiary timeframe", "General")

        self._periods = [5, 8, 13, 21, 34]
        self._smas = [[None] * 5 for _ in range(3)]
        # Previous values: [tf_index][sma_index]
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

    def process_tf1(self, candle, v0, v1, v2, v3, v4):
        if candle.State != CandleStates.Finished:
            return
        self._process_tf_values(0, v0, v1, v2, v3, v4)
        if self._is_ready[0]:
            self._bars_since_trade += 1
            self._evaluate_signal()

    def process_tf2(self, candle, v0, v1, v2, v3, v4):
        if candle.State != CandleStates.Finished:
            return
        self._process_tf_values(1, v0, v1, v2, v3, v4)

    def process_tf3(self, candle, v0, v1, v2, v3, v4):
        if candle.State != CandleStates.Finished:
            return
        self._process_tf_values(2, v0, v1, v2, v3, v4)

    def _process_tf_values(self, tf_index, v0, v1, v2, v3, v4):
        vals = [float(v0), float(v1), float(v2), float(v3), float(v4)]

        for i in range(5):
            if vals[i] == 0.0:
                return

        # Get previous values
        prevs = [self._previous[tf_index][i] for i in range(5)]

        is_ready = True
        for i in range(5):
            if prevs[i] == 0.0:
                is_ready = False

        # Store current values
        for i in range(5):
            self._previous[tf_index][i] = vals[i]

        if not is_ready:
            return

        up = 0
        down = 0
        for i in range(5):
            if vals[i] > prevs[i]:
                up += 1
            elif vals[i] < prevs[i]:
                down += 1

        up_pct = up / 5.0 * 100.0
        down_pct = down / 5.0 * 100.0

        self._uitog[tf_index] = up_pct
        self._ditog[tf_index] = down_pct
        self._is_ready[tf_index] = True

    def _evaluate_signal(self):
        if not self._is_ready[0] or not self._is_ready[1] or not self._is_ready[2]:
            return

        # Compute average trend strength across all 3 timeframes
        avg_up = (self._uitog[0] + self._uitog[1] + self._uitog[2]) / 3.0
        avg_down = (self._ditog[0] + self._ditog[1] + self._ditog[2]) / 3.0

        # Determine signal based on average trend strength
        self._signal = 0

        if avg_up >= 80.0:
            self._signal = 2
        elif avg_down >= 80.0:
            self._signal = -2
        elif avg_up >= 60.0:
            self._signal = 1
        elif avg_down >= 60.0:
            self._signal = -1

        if self._bars_since_trade < self.cooldown_bars:
            return

        # Close logic: close when signal reverses strongly
        if self.Position > 0 and self._signal <= -2:
            self.SellMarket()
            self._bars_since_trade = 0
            return

        if self.Position < 0 and self._signal >= 2:
            self.BuyMarket()
            self._bars_since_trade = 0
            return

        # Open logic: open when signal is strong (level 2)
        if self._signal >= 2 and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_trade = 0
        elif self._signal <= -2 and self.Position >= 0:
            self.SellMarket()
            self._bars_since_trade = 0

    def CreateClone(self):
        return sma_trend_filter_strategy()
