import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_long_strategy(Strategy):
    """
    Bollinger Bands Long strategy.
    Long only: buys when the close is below the lower Bollinger band and RSI is below RsiOversold, and closes the long once the close
    is at or above the middle band.
    """

    def __init__(self):
        super(bollinger_bands_long_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 10).SetGreaterThanZero().SetDisplay("BB Length", "Bollinger period", "Bollinger")
        self._bb_deviation = self.Param("BbDeviation", 2.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Bollinger standard deviation multiplier", "Bollinger")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bollinger_bands_long_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_deviation.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, bollinger_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not rsi_value.IsFormed:
            return

        lower = bollinger_value.LowBand
        middle = bollinger_value.MovingAverage
        if lower is None or middle is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        rsi = rsi_value.GetValue[Decimal](None)

        if self.Position == 0 and close < lower and rsi < Decimal(self._rsi_oversold.Value):
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and close >= middle:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return bollinger_bands_long_strategy()
