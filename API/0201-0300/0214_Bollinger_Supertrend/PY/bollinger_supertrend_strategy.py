import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SuperTrend
from StockSharp.Algo.Strategies import Strategy

class bollinger_supertrend_strategy(Strategy):
    """
    Bollinger Supertrend strategy.
    A close above the upper Bollinger band that is also above the Supertrend line goes long and a close below the lower band that is also
    below the Supertrend goes short, reversing an opposite position. The Supertrend is the trailing stop: a long closes when price closes
    below the line and a short when it closes above it.
    """

    def __init__(self):
        super(bollinger_supertrend_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("BB Period", "Period of the Bollinger Bands", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Standard deviation multiplier of the bands", "Bollinger")
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetGreaterThanZero().SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bollinger_supertrend_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, supertrend, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not supertrend_value.IsFormed:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        line = supertrend_value.Value
        close = candle.ClosePrice

        if close > upper and close > line and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower and close < line and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < line:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > line:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_supertrend_strategy()
