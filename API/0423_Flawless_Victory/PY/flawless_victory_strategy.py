import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy

class flawless_victory_strategy(Strategy):
    """
    FlawlessVictory: Bollinger Bands + RSI mean reversion.
    Buys when price below lower BB with RSI oversold.
    Sells when price above upper BB with RSI overbought.
    Exits at middle band.
    """

    def __init__(self):
        super(flawless_victory_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type for strategy calculation", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_width = self.Param("BBWidth", 1.5) \
            .SetDisplay("BB Width", "Bollinger Bands standard deviation", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_oversold = self.Param("RSIOversold", 42.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._rsi_overbought = self.Param("RSIOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(flawless_victory_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(flawless_victory_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = self._bb_width.Value

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        middle = bb_value.MovingAverage

        if upper is None or lower is None or middle is None:
            return

        if rsi_value.IsEmpty:
            return

        upper = float(upper)
        lower = float(lower)
        middle = float(middle)
        rsi = float(IndicatorHelper.ToDecimal(rsi_value))

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)

        if close < lower and rsi < self._rsi_oversold.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif close > upper and rsi > self._rsi_overbought.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position > 0 and close >= middle:
            self.SellMarket()
            self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return flawless_victory_strategy()
