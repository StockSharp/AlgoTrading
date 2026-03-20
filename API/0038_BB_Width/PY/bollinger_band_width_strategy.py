import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class bollinger_band_width_strategy(Strategy):
    """
    Strategy that trades on Bollinger Bands Width expansion.
    Identifies periods of increasing volatility (widening Bollinger Bands)
    and trades in the direction of the trend as identified by price position relative to the middle band.
    """

    def __init__(self):
        super(bollinger_band_width_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20).SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bb_deviation = self.Param("BollingerDeviation", 2.0).SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_width = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_band_width_strategy, self).OnReseted()
        self._prev_width = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(bollinger_band_width_strategy, self).OnStarted(time)

        self._prev_width = 0.0
        self._cooldown = 0

        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed:
            return

        if bb_val.UpBand is None or bb_val.LowBand is None or bb_val.MovingAverage is None:
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)
        middle = float(bb_val.MovingAverage)
        bb_width = upper - lower
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self._prev_width == 0:
            self._prev_width = bb_width
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_width = bb_width
            return

        is_expanding = bb_width > self._prev_width

        if self.Position == 0 and is_expanding:
            if close > middle:
                self.BuyMarket()
                self._cooldown = cd
            else:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and not is_expanding:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and not is_expanding:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_width = bb_width

    def CreateClone(self):
        return bollinger_band_width_strategy()
