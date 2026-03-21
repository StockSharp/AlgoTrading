import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_expansion_strategy(Strategy):
    """
    Strategy that trades on volatility expansion as measured by ATR.
    Enters when ATR expands above threshold and price is above/below MA,
    exits when volatility contracts.
    """

    def __init__(self):
        super(atr_expansion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for MA calculation", "Indicators")
        self._atr_expansion_ratio = self.Param("AtrExpansionRatio", 1.05).SetDisplay("Expansion Ratio", "ATR expansion ratio for entry signal", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")
        self._lookback = self.Param("Lookback", 5).SetDisplay("Lookback", "Bars to look back for ATR comparison", "General")

        self._prev_atr = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_expansion_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(atr_expansion_strategy, self).OnStarted(time)

        self._prev_atr = 0.0
        self._has_prev = False
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)

        if not self._has_prev:
            self._prev_atr = av
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_atr = av
            return

        ratio = float(self._atr_expansion_ratio.Value)
        is_expanding = self._prev_atr > 0 and av / self._prev_atr >= ratio
        is_contracting = self._prev_atr > 0 and av / self._prev_atr < 1.0 / ratio
        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and is_expanding:
            if close > sv:
                self.BuyMarket()
                self._cooldown = cd
            elif close < sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and is_contracting:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and is_contracting:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_atr = av

    def CreateClone(self):
        return atr_expansion_strategy()
