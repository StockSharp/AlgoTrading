import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class eighty_twenty_strategy(Strategy):
    """80-20 Strategy."""

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

        self._ema = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eighty_twenty_strategy, self).OnReseted()
        self._ema = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(eighty_twenty_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        rng = high - low

        if rng <= 0:
            return

        offset = float(self._range_percent.Value) * rng
        cooldown = int(self._cooldown_bars.Value)

        trigger_green = close >= high - offset and open_p <= low + offset
        trigger_red = open_p >= high - offset and close <= low + offset

        if trigger_green and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif trigger_red and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return eighty_twenty_strategy()
