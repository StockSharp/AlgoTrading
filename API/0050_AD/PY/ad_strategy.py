import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AccumulationDistributionLine
from StockSharp.Algo.Strategies import Strategy

class ad_strategy(Strategy):
    """
    Accumulation/Distribution (A/D) Strategy.
    Long entry: A/D rising and price above MA.
    Short entry: A/D falling and price below MA.
    """

    def __init__(self):
        super(ad_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_ad = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ad_strategy, self).OnReseted()
        self._previous_ad = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ad_strategy, self).OnStarted2(time)

        self._previous_ad = 0.0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        ad = AccumulationDistributionLine()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, ad, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, ad_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(ad_val)

        if self._previous_ad == 0:
            self._previous_ad = av
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_ad = av
            return

        ad_rising = av > self._previous_ad
        close = float(candle.ClosePrice)
        mv = float(ma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if ad_rising and close > mv:
                self.BuyMarket()
                self._cooldown = cd
            elif not ad_rising and close < mv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and not ad_rising:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and ad_rising:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_ad = av

    def CreateClone(self):
        return ad_strategy()
