import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_channel_based_grid_strategy(Strategy):
    def __init__(self):
        super(keltner_channel_based_grid_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "MA and ATR period", "Keltner")
        self._grid_coeff = self.Param("GridCoefficient", 1.33) \
            .SetGreaterThanZero() \
            .SetDisplay("Grid coeff", "Multiplier for channel width", "Keltner")
        self._num_grids = self.Param("NumGrids", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Grids", "Number of grid levels", "Strategy")
        self._max_rebalances = self.Param("MaxRebalances", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Rebalances", "Maximum rebalance orders per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 240) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between rebalances", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle type", "Type of candles", "General")
        self._rebalance_count = 0
        self._bars_since_rebalance = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(keltner_channel_based_grid_strategy, self).OnReseted()
        self._rebalance_count = 0
        self._bars_since_rebalance = 0

    def OnStarted2(self, time):
        super(keltner_channel_based_grid_strategy, self).OnStarted2(time)
        self._rebalance_count = 0
        self._bars_since_rebalance = self._cooldown_bars.Value
        ma = ExponentialMovingAverage()
        ma.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_rebalance += 1
        ma_v = float(ma_val)
        atr_v = float(atr_val)
        if atr_v == 0.0:
            return
        close = float(candle.ClosePrice)
        gc = float(self._grid_coeff.Value)
        band_width = atr_v * gc
        kc_rate = (close - ma_v) / band_width
        target_pos = -kc_rate
        if self._rebalance_count < self._max_rebalances.Value and self._bars_since_rebalance >= self._cooldown_bars.Value:
            if target_pos > 0 and self.Position <= 0:
                self.BuyMarket()
                self._rebalance_count += 1
                self._bars_since_rebalance = 0
            elif target_pos < 0 and self.Position >= 0:
                self.SellMarket()
                self._rebalance_count += 1
                self._bars_since_rebalance = 0

    def CreateClone(self):
        return keltner_channel_based_grid_strategy()
