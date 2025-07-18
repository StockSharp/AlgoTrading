import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class cci_with_volatility_filter_strategy(Strategy):
    """
    Strategy based on CCI with Volatility Filter.
    """

    def __init__(self):
        super(cci_with_volatility_filter_strategy, self).__init__()

        # CCI period parameter.
        self._cciPeriod = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # ATR period parameter.
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # CCI oversold level parameter.
        self._cciOversold = self.Param("CciOversold", -100) \
            .SetDisplay("CCI Oversold", "CCI oversold level", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(-150, -50, 25)

        # CCI overbought level parameter.
        self._cciOverbought = self.Param("CciOverbought", 100) \
            .SetDisplay("CCI Overbought", "CCI overbought level", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 25)

        # Candle type parameter.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal indicators
        self._atrSma = None

    @property
    def CciPeriod(self):
        return self._cciPeriod.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cciPeriod.Value = value

    @property
    def AtrPeriod(self):
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def CciOversold(self):
        return self._cciOversold.Value

    @CciOversold.setter
    def CciOversold(self, value):
        self._cciOversold.Value = value

    @property
    def CciOverbought(self):
        return self._cciOverbought.Value

    @CciOverbought.setter
    def CciOverbought(self, value):
        self._cciOverbought.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(cci_with_volatility_filter_strategy, self).OnStarted(time)

        # Create indicators
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        self._atrSma = SimpleMovingAverage()
        self._atrSma.Length = self.AtrPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.Bind(cci, atr, lambda candle, cci_value, atr_value: self._on_candle(candle, cci_value, atr_value)).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
    def _on_candle(self, candle, cci_value, atr_value):
        # Calculate ATR average
        atr_avg = float(
            process_float(
                self._atrSma,
                atr_value,
                candle.ServerTime,
                candle.State == CandleStates.Finished,
            )
        )

        # Process the strategy logic
        self.ProcessStrategy(candle, cci_value, atr_value, atr_avg)

    def ProcessStrategy(self, candle, cciValue, atrValue, atrAvg):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check volatility - only trade in low volatility environment
        isLowVolatility = atrValue < atrAvg

        # Trading logic - only enter during low volatility
        if isLowVolatility:
            if cciValue < self.CciOversold and self.Position <= 0:
                # CCI oversold in low volatility - Go long
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter long position
                self.BuyMarket(volume)
            elif cciValue > self.CciOverbought and self.Position >= 0:
                # CCI overbought in low volatility - Go short
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter short position
                self.SellMarket(volume)

        # Exit logic - when CCI crosses over zero
        if (self.Position > 0 and cciValue > 0) or (self.Position < 0 and cciValue < 0):
            # Close position
            self.ClosePosition()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cci_with_volatility_filter_strategy()

