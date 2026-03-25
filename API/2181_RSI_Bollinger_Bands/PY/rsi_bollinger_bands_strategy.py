import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class rsi_bollinger_bands_strategy(Strategy):
    def __init__(self):
        super(rsi_bollinger_bands_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation length", "Indicators")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Bollinger bands length", "Indicators")
        self._bollinger_width = self.Param("BollingerWidth", 2.0) \
            .SetDisplay("Bollinger Width", "Band width multiplier", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("RSI Oversold", "Buy threshold", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("RSI Overbought", "Sell threshold", "Indicators")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_width(self):
        return self._bollinger_width.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    def OnReseted(self):
        super(rsi_bollinger_bands_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(rsi_bollinger_bands_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, bollinger, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return
        rsi = float(rsi_value)

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        close = float(candle.ClosePrice)

        buy_signal = rsi < float(self.rsi_oversold) and close < lower
        sell_signal = rsi > float(self.rsi_overbought) and close > upper

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return rsi_bollinger_bands_strategy()
