import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class liquidity_grab_volume_trap_strategy(Strategy):
    """
    Detects liquidity grabs where price sweeps beyond recent range
    then reverses back, indicating a trap.
    """

    def __init__(self):
        super(liquidity_grab_volume_trap_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 10) \
            .SetDisplay("Lookback", "Bars for range", "General")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles for calculations", "General")

        self._highest = None
        self._lowest = None
        self._bars_since_signal = 0
        self._prev_high = 0.0
        self._prev_low = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(liquidity_grab_volume_trap_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._bars_since_signal = 0
        self._prev_high = 0.0
        self._prev_low = 0.0

    def OnStarted2(self, time):
        super(liquidity_grab_volume_trap_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._lookback.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback.Value
        self._bars_since_signal = 0
        self._prev_high = 0.0
        self._prev_low = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_signal += 1
        hv = float(high_val)
        lv = float(low_val)

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            self._prev_high = hv
            self._prev_low = lv
            return

        range_high = self._prev_high
        range_low = self._prev_low
        self._prev_high = hv
        self._prev_low = lv

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        bull_grab = low < range_low and close > range_low
        bear_grab = high > range_high and close < range_high

        if self._bars_since_signal < self._cooldown_bars.Value:
            return

        if bull_grab and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        elif bear_grab and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0

    def CreateClone(self):
        return liquidity_grab_volume_trap_strategy()
