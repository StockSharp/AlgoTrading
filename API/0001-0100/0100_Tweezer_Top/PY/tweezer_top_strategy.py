import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class tweezer_top_strategy(Strategy):
    """
    Tweezer Top strategy.
    Enters short on Tweezer Top (bullish then bearish with matching highs).
    Enters long on Tweezer Bottom (bearish then bullish with matching lows).
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(tweezer_top_strategy, self).__init__()
        self._tolerance_percent = self.Param("TolerancePercent", 0.1).SetDisplay("Tolerance %", "Max diff between highs/lows", "Pattern")
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "Period of SMA for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tweezer_top_strategy, self).OnReseted()
        self._prev_candle = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(tweezer_top_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

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

        if self._prev_candle is None:
            self._prev_candle = candle
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            return

        tol = self._tolerance_percent.Value
        high_tolerance = float(self._prev_candle.HighPrice) * (tol / 100.0)
        low_tolerance = float(self._prev_candle.LowPrice) * (tol / 100.0)

        cd = self._cooldown_bars.Value
        sv = float(sma_val)

        # Tweezer Top: prev bullish, current bearish, matching highs
        is_tweezer_top = (
            self._prev_candle.ClosePrice > self._prev_candle.OpenPrice and
            candle.ClosePrice < candle.OpenPrice and
            abs(float(self._prev_candle.HighPrice) - float(candle.HighPrice)) <= high_tolerance
        )

        # Tweezer Bottom: prev bearish, current bullish, matching lows
        is_tweezer_bottom = (
            self._prev_candle.ClosePrice < self._prev_candle.OpenPrice and
            candle.ClosePrice > candle.OpenPrice and
            abs(float(self._prev_candle.LowPrice) - float(candle.LowPrice)) <= low_tolerance
        )

        if self.Position == 0 and is_tweezer_top:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_tweezer_bottom:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd

        self._prev_candle = candle

    def CreateClone(self):
        return tweezer_top_strategy()
