import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SMA, CCI
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ma_cci_strategy(Strategy):
    """
    Strategy combining Moving Average and CCI indicators.
    Buys when price is above MA and CCI is oversold.
    Sells when price is below MA and CCI is overbought.

    """

    def __init__(self):
        super(ma_cci_strategy, self).__init__()

        # Initialize strategy parameters
        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._cciPeriod = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._overboughtLevel = self.Param("OverboughtLevel", 100) \
            .SetDisplay("Overbought Level", "CCI level considered overbought", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(80, 150, 25)

        self._oversoldLevel = self.Param("OversoldLevel", -100) \
            .SetDisplay("Oversold Level", "CCI level considered oversold", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(-150, -80, 25)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 1.0)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def MaPeriod(self):
        """MA period."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def CciPeriod(self):
        """CCI period."""
        return self._cciPeriod.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cciPeriod.Value = value

    @property
    def OverboughtLevel(self):
        """CCI overbought level."""
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def OversoldLevel(self):
        """CCI oversold level."""
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ma_cci_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(ma_cci_strategy, self).OnStarted(time)

        # Create indicators
        ma = SMA(); ma.Length = self.MaPeriod
        cci = CCI(); cci.Length = self.CciPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(ma, cci, self.ProcessCandle).Start()

        # Enable stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)

            # Create second area for CCI
            cciArea = self.CreateChartArea()
            self.DrawIndicator(cciArea, cci)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, cci_value):
        """
        Process candle and execute trading logic
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic
        if candle.ClosePrice > ma_value and cci_value < self.OversoldLevel and self.Position <= 0:
            # Price above MA and CCI is oversold - Buy
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif candle.ClosePrice < ma_value and cci_value > self.OverboughtLevel and self.Position >= 0:
            # Price below MA and CCI is overbought - Sell
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        elif self.Position > 0 and candle.ClosePrice < ma_value:
            # Exit long position when price crosses below MA
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice > ma_value:
            # Exit short position when price crosses above MA
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ma_cci_strategy()

