import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class double_bottom_strategy(Strategy):
    """
    Double Bottom reversal strategy.
    Detects two similar bottoms and enters long on confirmation.
    Uses SMA for exit signal.
    """

    def __init__(self):
        super(double_bottom_strategy, self).__init__()
        self._distance = self.Param("Distance", 20).SetDisplay("Distance", "Bars between bottoms", "Pattern")
        self._similarity_pct = self.Param("SimilarityPercent", 1.0).SetDisplay("Similarity %", "Max % diff between bottoms", "Pattern")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for exit SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._recent_low = 0.0
        self._prev_low = 0.0
        self._bars_since_low = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_bottom_strategy, self).OnReseted()
        self._recent_low = 0.0
        self._prev_low = 0.0
        self._bars_since_low = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(double_bottom_strategy, self).OnStarted(time)

        self._recent_low = 0.0
        self._prev_low = 0.0
        self._bars_since_low = 0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _track_lows(self, candle):
        low = float(candle.LowPrice)
        if self._recent_low == 0 or low < self._recent_low:
            if self._recent_low > 0:
                self._prev_low = self._recent_low
            self._recent_low = low
            self._bars_since_low = 0
        else:
            self._bars_since_low += 1

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._track_lows(candle)
            return

        # Track new lows
        low = float(candle.LowPrice)
        if self._recent_low == 0 or low < self._recent_low:
            if self._recent_low > 0:
                self._prev_low = self._recent_low
            self._recent_low = low
            self._bars_since_low = 0
        else:
            self._bars_since_low += 1

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value
        dist = self._distance.Value
        sim = float(self._similarity_pct.Value)

        if self.Position == 0 and self._prev_low > 0 and self._bars_since_low >= dist:
            price_diff = abs((self._recent_low - self._prev_low) / self._prev_low * 100.0)
            if price_diff <= sim and close > sv:
                self.BuyMarket()
                self._cooldown = cd
                self._recent_low = 0.0
                self._prev_low = 0.0
            elif price_diff <= sim and close < sv:
                self.SellMarket()
                self._cooldown = cd
                self._recent_low = 0.0
                self._prev_low = 0.0
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return double_bottom_strategy()
