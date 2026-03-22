import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class liquidity_sweep_filter_strategy(Strategy):
    """
    Liquidity sweep filter based on SMA +/- StdDev bands.
    Buys on breakout above upper band, sells on break below lower.
    """

    def __init__(self):
        super(liquidity_sweep_filter_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Base period", "Trend")
        self._multiplier = self.Param("Multiplier", 0.3) \
            .SetDisplay("Multiplier", "Band width multiplier", "Trend")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._trend = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(liquidity_sweep_filter_strategy, self).OnReseted()
        self._trend = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(liquidity_sweep_filter_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self._length.Value
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self._length.Value
        self._trend = 0
        self._bars_since_signal = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._std_dev, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_signal += 1

        if not self._sma.IsFormed or not self._std_dev.IsFormed:
            return

        sma = float(sma_val)
        std = float(std_val)
        mult = self._multiplier.Value
        upper = sma + mult * std
        lower = sma - mult * std
        close = float(candle.ClosePrice)

        prev_trend = self._trend

        if close > upper:
            self._trend = 1
        elif close < lower:
            self._trend = -1
        else:
            self._trend = 0

        if self._bars_since_signal < self._cooldown_bars.Value:
            return

        if prev_trend != 1 and self._trend == 1 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        elif prev_trend != -1 and self._trend == -1 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0

    def CreateClone(self):
        return liquidity_sweep_filter_strategy()
