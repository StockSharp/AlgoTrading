import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class eighty_twenty_strategy(Strategy):
    """
    80-20 strategy: trades when price closes near extremes of the candle.
    Buys on strong bullish candles (close near high, open near low).
    Sells on strong bearish candles (open near high, close near low).
    Uses EMA as trend filter.
    """

    def __init__(self):
        super(eighty_twenty_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._range_percent = self.Param("RangePercent", 0.2) \
            .SetDisplay("Range Percent", "Fraction of candle range for trigger zone", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eighty_twenty_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(eighty_twenty_strategy, self).OnStarted(time)
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        rng = high - low

        if rng <= 0.0:
            return

        offset = self._range_percent.Value * rng

        trigger_green = close >= high - offset and open_p <= low + offset
        trigger_red = open_p >= high - offset and close <= low + offset

        if trigger_green and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif trigger_red and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return eighty_twenty_strategy()
