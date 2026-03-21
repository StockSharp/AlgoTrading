import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class bollinger_williams_r_strategy(Strategy):
    """
    Bollinger Bands + Williams %R strategy.
    Enters long when price is at lower band and Williams %R is oversold.
    Enters short when price is at upper band and Williams %R is overbought.
    """

    def __init__(self):
        super(bollinger_williams_r_strategy, self).__init__()

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        self._cooldown = 0
        self._was_below_lower = False
        self._was_above_upper = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(bollinger_williams_r_strategy, self).OnStarted(time)
        self._cooldown = 0
        self._was_below_lower = False
        self._was_above_upper = False

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = self._bollinger_deviation.Value

        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, williams_r, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            wr_area = self.CreateChartArea()
            if wr_area is not None:
                self.DrawIndicator(wr_area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value, wr_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)
        price = float(candle.ClosePrice)
        wr_dec = float(wr_value)

        is_below_lower = price <= lower_band * 1.001
        is_above_upper = price >= upper_band * 0.999

        if self._cooldown > 0:
            self._cooldown -= 1
            self._was_below_lower = is_below_lower
            self._was_above_upper = is_above_upper
            return

        if not self._was_below_lower and is_below_lower and wr_dec < -45 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 6
        elif not self._was_above_upper and is_above_upper and wr_dec > -55 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 6
        elif price >= middle_band and self.Position < 0:
            self.BuyMarket()
        elif price <= middle_band and self.Position > 0:
            self.SellMarket()

        self._was_below_lower = is_below_lower
        self._was_above_upper = is_above_upper

    def OnReseted(self):
        super(bollinger_williams_r_strategy, self).OnReseted()
        self._cooldown = 0
        self._was_below_lower = False
        self._was_above_upper = False

    def CreateClone(self):
        return bollinger_williams_r_strategy()
