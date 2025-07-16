import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_macd_strategy(Strategy):
    """
    Strategy that combines ADX and MACD indicators to identify strong trends
    and potential entry points.

    """

    def __init__(self):
        super(adx_macd_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adxPeriod = self.Param("AdxPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "ADX Settings") \
            .SetCanOptimize(True)

        self._adxThreshold = self.Param("AdxThreshold", 25) \
            .SetRange(15, 40) \
            .SetDisplay("ADX Threshold", "ADX threshold for trend strength", "ADX Settings") \
            .SetCanOptimize(True)

        self._macdFast = self.Param("MacdFast", 12) \
            .SetRange(5, 30) \
            .SetDisplay("MACD Fast", "Fast period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._macdSlow = self.Param("MacdSlow", 26) \
            .SetRange(10, 50) \
            .SetDisplay("MACD Slow", "Slow period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._macdSignal = self.Param("MacdSignal", 9) \
            .SetRange(3, 20) \
            .SetDisplay("MACD Signal", "Signal period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")

        self._adx = None
        self._macd = None
        self._atr = None

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def AdxPeriod(self):
        return self._adxPeriod.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adxPeriod.Value = value

    @property
    def AdxThreshold(self):
        return self._adxThreshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adxThreshold.Value = value

    @property
    def MacdFast(self):
        return self._macdFast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macdFast.Value = value

    @property
    def MacdSlow(self):
        return self._macdSlow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macdSlow.Value = value

    @property
    def MacdSignal(self):
        return self._macdSignal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macdSignal.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    def OnStarted(self, time):
        super(adx_macd_strategy, self).OnStarted(time)

        # Initialize indicators
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFast
        self._macd.Macd.LongMa.Length = self.MacdSlow
        self._macd.SignalMa.Length = self.MacdSignal

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the indicators and candle processor
        subscription.BindEx(self._adx, self._macd, self._atr, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Draw ADX in a separate area
            adxArea = self.CreateChartArea()
            self.DrawIndicator(adxArea, self._adx)

            # Draw MACD in a separate area
            macdArea = self.CreateChartArea()
            self.DrawIndicator(macdArea, self._macd)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adxValue, macdValue, atrValue):
        """
        Process incoming candle with indicator values.

        :param candle: Candle to process.
        :param adxValue: ADX value.
        :param macdValue: MACD value.
        :param atrValue: ATR value.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        typedAdx = adxValue
        if typedAdx.MovingAverage is None:
            return
        adxIndicatorValue = typedAdx.MovingAverage

        macdTyped = macdValue
        try:
            macdLine = macdTyped.Macd
            signalLine = macdTyped.Signal
        except AttributeError:
            return

        atrIndicatorValue = to_float(atrValue)

        # ADX trend strength check
        strongTrend = adxIndicatorValue > self.AdxThreshold

        # Calculate stop loss distance based on ATR
        stopLossDistance = atrIndicatorValue * self.AtrMultiplier

        # Trading logic
        if strongTrend:
            if macdLine > signalLine:  # Bullish signal
                # Strong uptrend with bullish MACD - Long signal
                if self.Position <= 0:
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Buy signal: Strong trend (ADX={0:F2}) with bullish MACD ({1:F4} > {2:F4})".format(
                        adxIndicatorValue, macdLine, signalLine))

                    # Set stop loss
                    stopPrice = candle.ClosePrice - stopLossDistance
                    self.RegisterOrder(self.CreateOrder(Sides.Sell, stopPrice, Math.Abs(self.Position + self.Volume).Max(self.Volume)))
            elif macdLine < signalLine:  # Bearish signal
                # Strong downtrend with bearish MACD - Short signal
                if self.Position >= 0:
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Sell signal: Strong trend (ADX={0:F2}) with bearish MACD ({1:F4} < {2:F4})".format(
                        adxIndicatorValue, macdLine, signalLine))

                    # Set stop loss
                    stopPrice = candle.ClosePrice + stopLossDistance
                    self.RegisterOrder(self.CreateOrder(Sides.Buy, stopPrice, Math.Abs(self.Position + self.Volume).Max(self.Volume)))

        # Exit conditions
        if adxIndicatorValue < self.AdxThreshold * 0.8:
            if self.Position != 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Exit long: Trend weakening (ADX={0:F2})".format(adxIndicatorValue))
                elif self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Exit short: Trend weakening (ADX={0:F2})".format(adxIndicatorValue))

                # Cancel any pending stop orders
                self.CancelActiveOrders()
        # Additional exit logic for MACD crossover against the position
        elif (self.Position > 0 and macdLine < signalLine) or (self.Position < 0 and macdLine > signalLine):
            if self.Position != 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Exit long: MACD crossed below signal ({0:F4} < {1:F4})".format(macdLine, signalLine))
                elif self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Exit short: MACD crossed above signal ({0:F4} > {1:F4})".format(macdLine, signalLine))

                # Cancel any pending stop orders
                self.CancelActiveOrders()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_macd_strategy()
