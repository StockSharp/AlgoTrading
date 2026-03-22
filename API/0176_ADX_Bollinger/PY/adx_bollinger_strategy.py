import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import AverageDirectionalIndex, BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_bollinger_strategy(Strategy):
    """
    Strategy based on ADX and Bollinger Bands indicators.
    Enters long when ADX > 25 and price breaks above upper Bollinger band
    Enters short when ADX > 25 and price breaks below lower Bollinger band
    """

    def __init__(self):
        super(adx_bollinger_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_bollinger_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(adx_bollinger_strategy, self).OnStarted(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = self._bollinger_deviation.Value

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, bollinger, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)

            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return
        adx_ma_f = float(adx_ma)

        upper_band = float(bollinger_value.UpBand)
        lower_band = float(bollinger_value.LowBand)
        middle_band = (upper_band - lower_band) / 2.0 + lower_band

        price = float(candle.ClosePrice)
        stop_size = float(atr_value) * float(self._atr_multiplier.Value)

        # Trading logic
        if adx_ma_f > 25:  # Strong trend
            if price > upper_band and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))

                stop_price = price - stop_size
                stop_vol = max(abs(self.Position + self.Volume), self.Volume)
                self.RegisterOrder(self.CreateOrder(Sides.Sell, stop_price, stop_vol))
            elif price < lower_band and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

                stop_price = price + stop_size
                stop_vol = max(abs(self.Position + self.Volume), self.Volume)
                self.RegisterOrder(self.CreateOrder(Sides.Buy, stop_price, stop_vol))
        elif adx_ma_f < 20:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
        elif price < middle_band and self.Position > 0:
            self.SellMarket(self.Position)
        elif price > middle_band and self.Position < 0:
            self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return adx_bollinger_strategy()
