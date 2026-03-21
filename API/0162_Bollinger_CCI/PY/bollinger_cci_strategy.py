import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class bollinger_cci_strategy(Strategy):
    """
    Bollinger Bands + CCI strategy.
    Buy when price is below lower BB and CCI is oversold.
    Sell when price is above upper BB and CCI is overbought.
    """

    def __init__(self):
        super(bollinger_cci_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger Parameters")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Bollinger Parameters")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")
        self._cci_oversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")
        self._cci_overbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 80) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(bollinger_cci_strategy, self).OnStarted(time)
        self._cooldown = 0

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = self._bollinger_deviation.Value

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, cci, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if not bb_value.IsFormed or not cci_value.IsFormed:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)
        cci_dec = float(cci_value)
        price = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        os_level = self._cci_oversold.Value
        ob_level = self._cci_overbought.Value

        lower_touch = price <= lower_band * 1.002
        upper_touch = price >= upper_band * 0.998

        if lower_touch and cci_dec < os_level and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif upper_touch and cci_dec > ob_level and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd
        elif price > middle_band and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif price < middle_band and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

    def OnReseted(self):
        super(bollinger_cci_strategy, self).OnReseted()
        self._cooldown = 0

    def CreateClone(self):
        return bollinger_cci_strategy()
