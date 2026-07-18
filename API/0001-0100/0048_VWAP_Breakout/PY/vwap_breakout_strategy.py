import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vwap_breakout_strategy(Strategy):
    """
    VWAP Breakout strategy.
    Enters long when price breaks above VWAP, short when below.
    """

    def __init__(self):
        super(vwap_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_close = 0.0
        self._previous_vwap = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_breakout_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._previous_vwap = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vwap_breakout_strategy, self).OnStarted2(time)

        self._previous_close = 0.0
        self._previous_vwap = 0.0
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

        close = float(candle.ClosePrice)
        vp = float(vwap_val)

        if self._previous_close == 0:
            self._previous_close = close
            self._previous_vwap = vp
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_close = close
            self._previous_vwap = vp
            return

        cd = self._cooldown_bars.Value
        breakout_up = self._previous_close <= self._previous_vwap and close > vp
        breakout_down = self._previous_close >= self._previous_vwap and close < vp

        if self.Position == 0 and breakout_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and breakout_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < vp:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > vp:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_close = close
        self._previous_vwap = vp

    def CreateClone(self):
        return vwap_breakout_strategy()
