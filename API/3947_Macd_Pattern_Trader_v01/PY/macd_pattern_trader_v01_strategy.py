import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_pattern_trader_v01_strategy(Strategy):
    """
    MACD hook pattern trader: detects MACD hook reversals for entries.
    Simplified from the full C# version (no partial exits/volume management).
    """

    def __init__(self):
        super(macd_pattern_trader_v01_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFastPeriod", 5).SetDisplay("MACD Fast", "Fast EMA", "Indicators")
        self._macd_slow = self.Param("MacdSlowPeriod", 13).SetDisplay("MACD Slow", "Slow EMA", "Indicators")
        self._bearish_threshold = self.Param("BearishThreshold", 50.0).SetDisplay("Bearish Threshold", "Arms short trades", "Signals")
        self._bullish_threshold = self.Param("BullishThreshold", -50.0).SetDisplay("Bullish Threshold", "Arms long trades", "Signals")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bearish_armed = False
        self._bullish_armed = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_v01_strategy, self).OnReseted()
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bearish_armed = False
        self._bullish_armed = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_pattern_trader_v01_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self._macd_fast.Value
        macd.LongMa.Length = self._macd_slow.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return
        macd_line = float(macd_val)
        self._macd_prev3 = self._macd_prev2
        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = macd_line
        if self._macd_prev3 is None:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        curr = self._macd_prev1
        last = self._macd_prev2
        last3 = self._macd_prev3
        bt = float(self._bearish_threshold.Value)
        blt = float(self._bullish_threshold.Value)
        if curr > bt:
            self._bearish_armed = True
        if curr < 0:
            self._bearish_armed = False
        if self._bearish_armed and curr < bt and curr < last and last > last3 and curr > 0 and last3 < bt:
            if self.Position >= 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
                self._bearish_armed = False
                return
        if curr < blt:
            self._bullish_armed = True
        if curr > 0:
            self._bullish_armed = False
        if self._bullish_armed and curr > blt and curr < 0 and curr > last and last < last3 and last3 > blt:
            if self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
                self._bullish_armed = False

    def CreateClone(self):
        return macd_pattern_trader_v01_strategy()
