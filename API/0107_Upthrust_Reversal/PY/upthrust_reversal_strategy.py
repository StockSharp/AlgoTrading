import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class upthrust_reversal_strategy(Strategy):
    """
    Upthrust Reversal strategy (Wyckoff).
    Enters short when price spikes above recent resistance then closes back below it.
    Enters long when price dips below recent support then closes back above it.
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(upthrust_reversal_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetDisplay("Lookback", "Period for support/resistance", "Range")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period for SMA exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._highs = []
        self._lows = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(upthrust_reversal_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(upthrust_reversal_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
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

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._lookback_period.Value

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > lookback + 1:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < lookback + 1:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        sv = float(sma_val)

        # Find resistance and support from previous N bars
        resistance = max(self._highs[:-1])
        support = min(self._lows[:-1])

        # Upthrust: price spikes above resistance but closes below it (bearish)
        is_upthrust = (
            float(candle.HighPrice) > resistance and
            float(candle.ClosePrice) < resistance and
            candle.ClosePrice < candle.OpenPrice
        )

        # Spring: price dips below support but closes above it (bullish)
        is_spring = (
            float(candle.LowPrice) < support and
            float(candle.ClosePrice) > support and
            candle.ClosePrice > candle.OpenPrice
        )

        if self.Position == 0 and is_upthrust:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_spring:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd

    def CreateClone(self):
        return upthrust_reversal_strategy()
