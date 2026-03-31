import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class golden_ratio_cubes_strategy(Strategy):
    """Golden Ratio Cubes Strategy."""

    def __init__(self):
        super(golden_ratio_cubes_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 34) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Golden Ratio")
        self._phi = self.Param("Phi", 1.618) \
            .SetDisplay("Phi", "Golden ratio multiplier", "Golden Ratio")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._bb = None
        self._ema = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(golden_ratio_cubes_strategy, self).OnReseted()
        self._bb = None
        self._ema = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(golden_ratio_cubes_strategy, self).OnStarted2(time)

        bb_len = int(self._bb_length.Value)

        self._bb = BollingerBands()
        self._bb.Length = bb_len
        self._bb.Width = 2.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = bb_len

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed or not self._ema.IsFormed:
            return

        if bb_value.IsEmpty or ema_value.IsEmpty:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        mid = float(bb_value.MovingAverage)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if price > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price < mid:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > mid:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return golden_ratio_cubes_strategy()
