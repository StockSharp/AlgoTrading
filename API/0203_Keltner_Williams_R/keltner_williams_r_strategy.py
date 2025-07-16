import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import KeltnerChannels, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_williams_r_strategy(Strategy):
    """Strategy based on Keltner Channels and Williams %R indicators (#203)"""

    def __init__(self):
        super(keltner_williams_r_strategy, self).__init__()

        # Initialize strategy parameters
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Indicators") \
            .SetCanOptimize(True)

        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Indicators") \
            .SetCanOptimize(True)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel", "Indicators") \
            .SetCanOptimize(True)

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def ema_period(self):
        """EMA period for Keltner Channel"""
        return self._ema_period.Value

    @ema_period.setter
    def ema_period(self, value):
        self._ema_period.Value = value

    @property
    def keltner_multiplier(self):
        """Keltner Channel multiplier (k)"""
        return self._keltner_multiplier.Value

    @keltner_multiplier.setter
    def keltner_multiplier(self, value):
        self._keltner_multiplier.Value = value

    @property
    def atr_period(self):
        """ATR period for Keltner Channel"""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def williams_r_period(self):
        """Williams %R period"""
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(keltner_williams_r_strategy, self).OnStarted(time)

        # Initialize indicators
        keltner = KeltnerChannels()
        keltner.Length = self.ema_period
        keltner.Multiplier = self.keltner_multiplier

        williams_r = WilliamsR()
        williams_r.Length = self.williams_r_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner, williams_r, self.ProcessIndicators).Start()

        # Enable stop-loss protection based on ATR
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(2, UnitTypes.Absolute)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, keltner_value, williams_r_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        keltner_typed = keltner_value  # KeltnerChannelsValue
        upper = keltner_typed.Upper
        lower = keltner_typed.Lower
        middle = keltner_typed.Middle

        williams_r = float(williams_r_value)

        price = candle.ClosePrice

        # Trading logic:
        # Long: Price < lower Keltner band && Williams %R < -80 (oversold at lower band)
        # Short: Price > upper Keltner band && Williams %R > -20 (overbought at upper band)

        if price < lower and williams_r < -80 and self.Position <= 0:
            # Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif price > upper and williams_r > -20 and self.Position >= 0:
            # Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions
        elif self.Position > 0 and price > middle:
            # Exit long position when price returns to middle band
            self.SellMarket(self.Position)
        elif self.Position < 0 and price < middle:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_williams_r_strategy()

