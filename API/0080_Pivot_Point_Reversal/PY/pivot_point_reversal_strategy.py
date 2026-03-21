import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pivot_point_reversal_strategy(Strategy):
    """
    Pivot Point Reversal strategy.
    Calculates pivot points from a rolling window of highs, lows, closes.
    P = (H + L + C) / 3, S1 = 2*P - H, R1 = 2*P - L
    Buys on bounce off S1, sells on bounce off R1, exits at pivot.
    """

    def __init__(self):
        super(pivot_point_reversal_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 60).SetDisplay("Lookback", "Lookback for pivot calc", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pivot_point_reversal_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(pivot_point_reversal_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._closes = []
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        self._closes.append(float(candle.ClosePrice))

        lb = self._lookback.Value

        if len(self._highs) > lb:
            self._highs.pop(0)
            self._lows.pop(0)
            self._closes.pop(0)

        if len(self._highs) < lb:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Calculate pivot points from lookback window
        high = max(self._highs)
        low = min(self._lows)
        close = self._closes[-1]

        pivot = (high + low + close) / 3.0
        r1 = 2.0 * pivot - low
        s1 = 2.0 * pivot - high
        buffer = (r1 - s1) * 0.02

        if buffer <= 0:
            return

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        cd = self._cooldown_bars.Value

        # Bounce off S1 (buy)
        if self.Position == 0 and float(candle.LowPrice) <= s1 + buffer and is_bullish:
            self.BuyMarket()
            self._cooldown = cd
        # Bounce off R1 (sell)
        elif self.Position == 0 and float(candle.HighPrice) >= r1 - buffer and is_bearish:
            self.SellMarket()
            self._cooldown = cd
        # Exit at pivot
        elif self.Position > 0 and float(candle.ClosePrice) > pivot:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) < pivot:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return pivot_point_reversal_strategy()
