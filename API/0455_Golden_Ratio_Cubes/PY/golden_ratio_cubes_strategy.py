import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class golden_ratio_cubes_strategy(Strategy):
    """Golden Ratio Cubes Strategy.
    Uses BB width as a range proxy and golden ratio extensions for breakout levels.
    Buys when price breaks above upper BB.
    Sells when price breaks below lower BB.
    Exits at middle band.
    """

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

        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(golden_ratio_cubes_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(golden_ratio_cubes_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = 2.0

        ema = ExponentialMovingAverage()
        ema.Length = self._bb_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        mid = bb_value.MovingAverage

        if upper is None or lower is None or mid is None:
            return

        upper = float(upper)
        lower = float(lower)
        mid = float(mid)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)

        # Buy: price breaks above upper BB
        if price > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell: price breaks below lower BB
        elif price < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit long: price returns to middle
        elif self.Position > 0 and price < mid:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        # Exit short: price returns to middle
        elif self.Position < 0 and price > mid:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return golden_ratio_cubes_strategy()
