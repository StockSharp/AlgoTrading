import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class opening_range_breakout_strategy(Strategy):
    """Opening Range Breakout Strategy."""

    def __init__(self):
        super(opening_range_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._bb = None
        self._ema = None
        self._cooldown_remaining = 0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(opening_range_breakout_strategy, self).OnReseted()
        self._bb = None
        self._ema = None
        self._cooldown_remaining = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(opening_range_breakout_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = int(self._bb_length.Value)
        self._bb.Width = 2.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb, self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bb)
            self.DrawIndicator(area, self._ema)
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
        ema_val = float(IndicatorHelper.ToDecimal(ema_value))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if price > upper and price > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = price
            self._cooldown_remaining = cooldown
        elif price < lower and price < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = price
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price < mid:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > mid:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return opening_range_breakout_strategy()
