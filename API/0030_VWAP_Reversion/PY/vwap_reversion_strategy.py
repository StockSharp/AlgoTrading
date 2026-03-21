import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vwap_reversion_strategy(Strategy):
    """
    VWAP Reversion strategy.
    Trades on deviations from VWAP, exits when price returns.
    """

    def __init__(self):
        super(vwap_reversion_strategy, self).__init__()
        self._deviation_percent = self.Param("DeviationPercent", 0.5).SetDisplay("Deviation %", "Deviation from VWAP for entry", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_reversion_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(vwap_reversion_strategy, self).OnStarted(time)

        self._cooldown = 0

        vwap = VolumeWeightedMovingAverage()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwap, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, vwap_val):
        if candle.State != CandleStates.Finished:
            return

        vv = float(vwap_val)
        if vv <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        ratio = (close - vv) / vv
        threshold = float(self._deviation_percent.Value) / 100.0
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if ratio < -threshold:
                self.BuyMarket()
                self._cooldown = cd
            elif ratio > threshold:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if close >= vv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if close <= vv:
                self.BuyMarket()
                self._cooldown = cd

    def CreateClone(self):
        return vwap_reversion_strategy()
