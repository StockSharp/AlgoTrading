import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class larry_connors_percent_b_strategy(Strategy):
    def __init__(self):
        super(larry_connors_percent_b_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation for Bollinger Bands", "Bollinger")
        self._low_percent_b = self.Param("LowPercentB", 0.35) \
            .SetDisplay("Low PctB", "Lower threshold for percent B", "Signals")
        self._high_percent_b = self.Param("HighPercentB", 0.8) \
            .SetDisplay("High PctB", "Upper threshold for percent B to exit", "Signals")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_pct_b1 = None
        self._prev_pct_b2 = None
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(larry_connors_percent_b_strategy, self).OnReseted()
        self._prev_pct_b1 = None
        self._prev_pct_b2 = None
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(larry_connors_percent_b_strategy, self).OnStarted2(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = self._bollinger_deviation.Value
        dummy_ema = ExponentialMovingAverage()
        dummy_ema.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, dummy_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_val, dummy_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        upper = bb_val.UpBand
        lower = bb_val.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        if upper == lower:
            return
        close = float(candle.ClosePrice)
        pct_b = (close - lower) / (upper - lower)
        low_th = float(self._low_percent_b.Value)
        high_th = float(self._high_percent_b.Value)
        if self._prev_pct_b1 is None:
            self._prev_pct_b2 = self._prev_pct_b1
            self._prev_pct_b1 = pct_b
            return
        cond2 = self._prev_pct_b1 < low_th and pct_b < low_th
        if self.Position <= 0 and self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value and cond2:
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif self.Position > 0 and pct_b > high_th:
            self.SellMarket()
            self._bars_since_signal = 0
        self._prev_pct_b2 = self._prev_pct_b1
        self._prev_pct_b1 = pct_b

    def CreateClone(self):
        return larry_connors_percent_b_strategy()
