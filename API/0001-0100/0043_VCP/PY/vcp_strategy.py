import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class vcp_strategy(Strategy):
    """
    Volume Contraction Pattern (VCP) strategy.
    Looks for narrowing volatility (ATR declining) and breakouts above/below MA bands.
    """

    def __init__(self):
        super(vcp_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "ATR multiplier for breakout band", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_atr = 0.0
        self._contraction_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vcp_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._contraction_count = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vcp_strategy, self).OnStarted2(time)

        self._prev_atr = 0.0
        self._contraction_count = 0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)

        if self._prev_atr == 0:
            self._prev_atr = av
            return

        # Track contraction
        if av < self._prev_atr:
            self._contraction_count += 1
        else:
            self._contraction_count = 0

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_atr = av
            return

        mv = float(ma_val)
        mult = float(self._atr_multiplier.Value)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        is_contracted = self._contraction_count >= 3
        upper_band = mv + av * mult
        lower_band = mv - av * mult

        if self.Position == 0 and is_contracted:
            if close > upper_band:
                self.BuyMarket()
                self._cooldown = cd
                self._contraction_count = 0
            elif close < lower_band:
                self.SellMarket()
                self._cooldown = cd
                self._contraction_count = 0
        elif self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_atr = av

    def CreateClone(self):
        return vcp_strategy()
