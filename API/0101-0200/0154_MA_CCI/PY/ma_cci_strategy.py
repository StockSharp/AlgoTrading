import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ma_cci_strategy(Strategy):
    """
    Strategy combining Moving Average and CCI indicators.
    """

    def __init__(self):
        super(ma_cci_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 30) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 100.0) \
            .SetDisplay("Overbought Level", "CCI level considered overbought", "Trading Levels")
        self._oversold_level = self.Param("OversoldLevel", -100.0) \
            .SetDisplay("Oversold Level", "CCI level considered oversold", "Trading Levels")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._cci_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ma_cci_strategy, self).OnStarted2(time)
        self._cci_value = 0.0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)

        # CCI takes candle input - use BindEx as side handler
        subscription.BindEx(cci, self.OnCci)
        subscription.Bind(ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)

    def OnCci(self, candle, value):
        if not value.IsEmpty:
            self._cci_value = float(value)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        mv = float(ma_value)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        ob = self._overbought_level.Value
        os_level = self._oversold_level.Value

        # Buy: price above MA + CCI oversold
        if close > mv and self._cci_value < os_level and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif close < mv and self._cci_value > ob and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price crosses below MA
        if self.Position > 0 and close < mv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(ma_cci_strategy, self).OnReseted()
        self._cci_value = 0.0
        self._cooldown = 0

    def CreateClone(self):
        return ma_cci_strategy()
