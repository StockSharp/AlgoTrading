import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_weighted_price_breakout_strategy(Strategy):
    """
    Volume Weighted Price Breakout Strategy.
    Long entry: Price rises above VWMA.
    Short entry: Price falls below VWMA.
    Exit: Price crosses MA in the opposite direction.
    """

    def __init__(self):
        super(volume_weighted_price_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._vwap_period = self.Param("VWAPPeriod", 20).SetDisplay("VWAP Period", "Period for VWMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_weighted_price_breakout_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(volume_weighted_price_breakout_strategy, self).OnStarted(time)

        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        vwma = VolumeWeightedMovingAverage()
        vwma.Length = self._vwap_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, vwma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, vwma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, vwma_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        mv = float(ma_val)
        vv = float(vwma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if close > vv:
                self.BuyMarket()
                self._cooldown = cd
            elif close < vv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return volume_weighted_price_breakout_strategy()
