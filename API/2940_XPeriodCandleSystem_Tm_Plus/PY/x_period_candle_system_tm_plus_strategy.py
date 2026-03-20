import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class x_period_candle_system_tm_plus_strategy(Strategy):
    def __init__(self):
        super(x_period_candle_system_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def BbPeriod(self):
        return self._bb_period.Value

    @property
    def BbWidth(self):
        return self._bb_width.Value

    def OnStarted(self, time):
        super(x_period_candle_system_tm_plus_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BbPeriod
        bb.Width = self.BbWidth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        middle = bb_value.MovingAverage

        if upper is None or lower is None or middle is None:
            return

        upper = float(upper)
        lower = float(lower)
        middle = float(middle)

        close = float(candle.ClosePrice)
        is_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
        is_bearish = float(candle.ClosePrice) < float(candle.OpenPrice)

        # Buy: close above upper band with bullish candle
        if close > upper and is_bullish and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell: close below lower band with bearish candle
        elif close < lower and is_bearish and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long at middle band
        elif self.Position > 0 and close < middle:
            self.SellMarket()
        # Exit short at middle band
        elif self.Position < 0 and close > middle:
            self.BuyMarket()

    def CreateClone(self):
        return x_period_candle_system_tm_plus_strategy()
