import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vwma_strategy(Strategy):
    """
    Volume Weighted Moving Average (VWMA) Strategy.
    Long entry: Price crosses above VWMA.
    Short entry: Price crosses below VWMA.
    """

    def __init__(self):
        super(vwma_strategy, self).__init__()
        self._vwma_period = self.Param("VWMAPeriod", 14).SetDisplay("VWMA Period", "Period for Volume Weighted Moving Average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_close = 0.0
        self._previous_vwma = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwma_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._previous_vwma = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vwma_strategy, self).OnStarted(time)

        self._previous_close = 0.0
        self._previous_vwma = 0.0
        self._cooldown = 0

        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self._vwma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, vwma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        vp = float(vwma_val)

        if self._previous_close == 0:
            self._previous_close = close
            self._previous_vwma = vp
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_close = close
            self._previous_vwma = vp
            return

        cd = self._cooldown_bars.Value
        crossover_up = self._previous_close <= self._previous_vwma and close > vp
        crossover_down = self._previous_close >= self._previous_vwma and close < vp

        if self.Position == 0 and crossover_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and crossover_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and crossover_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and crossover_up:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_close = close
        self._previous_vwma = vp

    def CreateClone(self):
        return vwma_strategy()
