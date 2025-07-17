import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class keltner_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Keltner Channel width breakouts.
    When Keltner Channel width increases significantly above its average,
    it enters position in the direction determined by price movement.

    """

    def __init__(self):
        super(keltner_width_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._emaPeriod = self.Param("EMAPeriod", 20) \
            .SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators")

        self._atrPeriod = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators")

        self._atrMultiplier = self.Param("ATRMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")

        self._avgPeriod = self.Param("AvgPeriod", 20) \
            .SetDisplay("Average Period", "Period for Keltner width average calculation", "Indicators")

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopMultiplier = self.Param("StopMultiplier", 2) \
            .SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management")

        # Indicator fields
        self._ema = None
        self._atr = None
        self._widthAverage = None

        # Track channel width values
        self._lastWidth = 0
        self._lastAvgWidth = 0

        # Track EMA and ATR values to calculate channel
        self._currentEma = 0
        self._currentAtr = 0

        self._lastBid = 0
        self._lastAsk = 0

    @property
    def EMAPeriod(self):
        """EMA period for Keltner Channel."""
        return self._emaPeriod.Value

    @EMAPeriod.setter
    def EMAPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def ATRPeriod(self):
        """ATR period for Keltner Channel."""
        return self._atrPeriod.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def ATRMultiplier(self):
        """ATR multiplier for Keltner Channel."""
        return self._atrMultiplier.Value

    @ATRMultiplier.setter
    def ATRMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def AvgPeriod(self):
        """Period for width average calculation."""
        return self._avgPeriod.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avgPeriod.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for breakout detection."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopMultiplier(self):
        """Stop-loss ATR multiplier."""
        return self._stopMultiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stopMultiplier.Value = value

    def OnStarted(self, time):
        super(keltner_width_breakout_strategy, self).OnStarted(time)

        self._lastWidth = 0
        self._lastAvgWidth = 0
        self._currentEma = 0
        self._currentAtr = 0
        self._lastBid = 0
        self._lastAsk = 0

        # Create indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EMAPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.ATRPeriod
        self._widthAverage = SimpleMovingAverage()
        self._widthAverage.Length = self.AvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind to candle processing
        subscription.Bind(self.ProcessCandle).Start()

        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Subscribe to market depth (order book)
        self.SubscribeOrderBook() \
            .Bind(self.OnOrderBookReceived) \
            .Start()

    def OnOrderBookReceived(self, orderBook):
        # Get best bid and ask from order book
        bestBid = (
            float(orderBook.Bids[0].Price)
            if orderBook.Bids is not None and len(orderBook.Bids) > 0
            else 0
        )
        bestAsk = (
            float(orderBook.Asks[0].Price)
            if orderBook.Asks is not None and len(orderBook.Asks) > 0
            else 0
        )
        self._lastBid = bestBid
        self._lastAsk = bestAsk

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Process candle through EMA and ATR
        emaValue = process_candle(self._ema, candle)
        atrValue = process_candle(self._atr, candle)

        self._currentEma = to_float(emaValue)
        self._currentAtr = to_float(atrValue)

        # Calculate Keltner Channel boundaries
        upperBand = self._currentEma + self.ATRMultiplier * self._currentAtr
        lowerBand = self._currentEma - self.ATRMultiplier * self._currentAtr

        # Calculate Channel width
        width = upperBand - lowerBand

        # Process width through average
        widthAvgValue = to_float(process_float(self._widthAverage, width, candle.ServerTime, candle.State == CandleStates.Finished))
        avgWidth = widthAvgValue

        # For first values, just save and skip
        if self._lastWidth == 0:
            self._lastWidth = width
            self._lastAvgWidth = avgWidth
            return

        # Calculate width standard deviation (simplified approach)
        stdDev = abs(width - avgWidth) * 1.5  # Simplified approximation

        # Skip if indicators are not formed yet
        if not self._ema.IsFormed or not self._atr.IsFormed or not self._widthAverage.IsFormed:
            self._lastWidth = width
            self._lastAvgWidth = avgWidth
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._lastWidth = width
            self._lastAvgWidth = avgWidth
            return

        # Keltner width breakout detection
        if width > avgWidth + self.Multiplier * stdDev:
            # Determine direction based on price and bands
            priceDirection = False

            # If price is closer to upper band, go long. If closer to lower band, go short.
            upperDistance = float(abs(candle.ClosePrice - upperBand))
            lowerDistance = float(abs(candle.ClosePrice - lowerBand))

            if upperDistance < lowerDistance:
                # Price is closer to upper band, likely bullish
                priceDirection = True

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Calculate stop-loss based on current ATR
            stopOffset = self.StopMultiplier * self._currentAtr

            # Trade in the determined direction
            if priceDirection and self.Position <= 0:
                # Bullish direction - Buy
                buyPrice = self._lastAsk
                self.BuyMarket(self.Volume + Math.Abs(self.Position))

                # Set stop-loss order
                stopLoss = buyPrice - stopOffset
                self.RegisterOrder(self.CreateOrder(Sides.Sell, stopLoss, Math.Abs(self.Position)))
            elif not priceDirection and self.Position >= 0:
                # Bearish direction - Sell
                sellPrice = self._lastBid
                self.SellMarket(self.Volume + Math.Abs(self.Position))

                # Set stop-loss order
                stopLoss = sellPrice + stopOffset
                self.RegisterOrder(self.CreateOrder(Sides.Buy, stopLoss, Math.Abs(self.Position)))
        # Check for exit condition - width returns to average
        elif (self.Position > 0 or self.Position < 0) and width < avgWidth:
            # Exit position
            self.ClosePosition()

        # Update last values
        self._lastWidth = width
        self._lastAvgWidth = avgWidth

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_width_breakout_strategy()