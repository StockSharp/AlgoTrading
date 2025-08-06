import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, WeightedMovingAverage, SmoothedMovingAverage, HullMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class MovingAverageTypeEnum:
    """Enum for Moving Average types."""
    Simple = 0
    Exponential = 1
    Weighted = 2
    Smoothed = 3
    HullMA = 4

class ma_williams_r_strategy(Strategy):
    """
    Implementation of strategy - MA + Williams %R.
    Buy when price is above MA and Williams %R is below -80 (oversold).
    Sell when price is below MA and Williams %R is above -20 (overbought).
    """
    def __init__(self):
        super(ma_williams_r_strategy, self).__init__()

        # Initialize strategy parameters
        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average", "MA Parameters")

        self._maType = self.Param("MaType", MovingAverageTypeEnum.Simple) \
            .SetDisplay("MA Type", "Type of Moving Average", "MA Parameters")

        self._williamsRPeriod = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R Parameters")

        self._williamsROversold = self.Param("WilliamsROversold", -80) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Oversold", "Williams %R level to consider market oversold", "Williams %R Parameters")

        self._williamsROverbought = self.Param("WilliamsROverbought", -20) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Overbought", "Williams %R level to consider market overbought", "Williams %R Parameters")

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def MaPeriod(self):
        # Moving Average period.
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def MaType(self):
        # Moving Average type.
        return self._maType.Value

    @MaType.setter
    def MaType(self, value):
        self._maType.Value = value

    @property
    def WilliamsRPeriod(self):
        # Williams %R period.
        return self._williamsRPeriod.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williamsRPeriod.Value = value

    @property
    def WilliamsROversold(self):
        # Williams %R oversold level (usually below -80).
        return self._williamsROversold.Value

    @WilliamsROversold.setter
    def WilliamsROversold(self, value):
        self._williamsROversold.Value = value

    @property
    def WilliamsROverbought(self):
        # Williams %R overbought level (usually above -20).
        return self._williamsROverbought.Value

    @WilliamsROverbought.setter
    def WilliamsROverbought(self, value):
        self._williamsROverbought.Value = value

    @property
    def StopLoss(self):
        # Stop-loss value.
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        # Candle type used for strategy.
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(ma_williams_r_strategy, self).OnReseted()
        self.Indicators.Clear()

    def OnStarted(self, time):
        super(ma_williams_r_strategy, self).OnStarted(time)

        # Create indicators
        ma = None

        # Create MA based on selected type
        if self.MaType == MovingAverageTypeEnum.Exponential:
            ma = ExponentialMovingAverage()
            ma.Length = self.MaPeriod
        elif self.MaType == MovingAverageTypeEnum.Weighted:
            ma = WeightedMovingAverage()
            ma.Length = self.MaPeriod
        elif self.MaType == MovingAverageTypeEnum.Smoothed:
            ma = SmoothedMovingAverage()
            ma.Length = self.MaPeriod
        elif self.MaType == MovingAverageTypeEnum.HullMA:
            ma = HullMovingAverage()
            ma.Length = self.MaPeriod
        else:
            ma = SimpleMovingAverage()
            ma.Length = self.MaPeriod

        williamsR = WilliamsR()
        williamsR.Length = self.WilliamsRPeriod

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(ma, williamsR, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)

            # Create separate area for Williams %R
            oscillatorArea = self.CreateChartArea()
            if oscillatorArea is not None:
                self.DrawIndicator(oscillatorArea, williamsR)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss
        )
    def ProcessCandle(self, candle, maValue, williamsRValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Current price
        price = float(candle.ClosePrice)

        # Determine if price is above or below MA
        isPriceAboveMA = price > maValue

        self.LogInfo("Candle: {0}, Close: {1}, MA: {2}, Price > MA: {3}, Williams %R: {4}".format(
            candle.OpenTime, price, maValue, isPriceAboveMA, williamsRValue))

        # Trading rules
        if isPriceAboveMA and williamsRValue < self.WilliamsROversold and self.Position <= 0:
            # Buy signal - price above MA and Williams %R oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Price above MA and Williams %R oversold ({0} < {1}). Volume: {2}".format(
                williamsRValue, self.WilliamsROversold, volume))
        elif not isPriceAboveMA and williamsRValue > self.WilliamsROverbought and self.Position >= 0:
            # Sell signal - price below MA and Williams %R overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Price below MA and Williams %R overbought ({0} > {1}). Volume: {2}".format(
                williamsRValue, self.WilliamsROverbought, volume))
        # Exit conditions
        elif not isPriceAboveMA and self.Position > 0:
            # Exit long position when price falls below MA
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Price fell below MA. Position: {0}".format(self.Position))
        elif isPriceAboveMA and self.Position < 0:
            # Exit short position when price rises above MA
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price rose above MA. Position: {0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_williams_r_strategy()
