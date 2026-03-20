import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_deviation_strategy(Strategy):
    """
    MA Deviation strategy.
    Trades when price deviates significantly from its moving average.
    """

    def __init__(self):
        super(ma_deviation_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._deviation_percent = self.Param("DeviationPercent", 2.0).SetDisplay("Deviation %", "Deviation percentage from MA required for entry", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_deviation_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_deviation_strategy, self).OnStarted(time)

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

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mv = float(ma_val)
        if mv == 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        deviation = (close - mv) / mv * 100.0
        threshold = float(self._deviation_percent.Value)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if deviation < -threshold:
                self.BuyMarket()
                self._cooldown = cd
            elif deviation > threshold:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close >= mv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close <= mv:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return ma_deviation_strategy()
