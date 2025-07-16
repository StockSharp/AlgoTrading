import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class lunch_break_fade_strategy(Strategy):
    """
    Strategy that trades on the price movement fade during the lunch break.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(lunch_break_fade_strategy, self).__init__()

        # Data type for candles.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Hour when lunch break typically starts (default is 13:00).
        self._lunchHour = self.Param("LunchHour", 13) \
            .SetRange(8, 16) \
            .SetDisplay("Lunch Hour", "Hour when lunch break typically starts (24-hour format)", "General")

        # Stop loss percentage from entry price.
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        # Store previous candles data for trend detection
        self._previousCandleClose = None
        self._twoCandlesBackClose = None

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def LunchHour(self):
        return self._lunchHour.Value

    @LunchHour.setter
    def LunchHour(self, value):
        self._lunchHour.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        super(lunch_break_fade_strategy, self).OnReseted()
        self._previousCandleClose = None
        self._twoCandlesBackClose = None

    def OnStarted(self, time):
        super(lunch_break_fade_strategy, self).OnStarted(time)

        self._previousCandleClose = None
        self._twoCandlesBackClose = None

        # Set up stop loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Create candle subscription for the specified timeframe
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the candle processor
        subscription.Bind(self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """
        Process incoming candle.

        :param candle: Candle to process.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if current hour is lunch hour
        if candle.OpenTime.Hour != self.LunchHour:
            # Update previous candles data
            self._twoCandlesBackClose = self._previousCandleClose
            self._previousCandleClose = candle.ClosePrice
            return

        # Trading logic can only be applied if we have enough historical data
        if self._previousCandleClose is None or self._twoCandlesBackClose is None:
            self._twoCandlesBackClose = self._previousCandleClose
            self._previousCandleClose = candle.ClosePrice
            return

        # Lunch break fade logic:

        # Check for price movement before lunch break
        priorUptrend = self._previousCandleClose > self._twoCandlesBackClose

        # Check current candle direction
        currentBullish = candle.ClosePrice > candle.OpenPrice

        # Look for fade pattern
        if priorUptrend and not currentBullish:
            # Uptrend before lunch, bearish candle at lunch - Sell signal
            if self.Position >= 0:
                # Close any long positions and go short
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Lunch break fade signal: Selling after uptrend")
        elif not priorUptrend and currentBullish:
            # Downtrend before lunch, bullish candle at lunch - Buy signal
            if self.Position <= 0:
                # Close any short positions and go long
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Lunch break fade signal: Buying after downtrend")

        # Update previous candles data
        self._twoCandlesBackClose = self._previousCandleClose
        self._previousCandleClose = candle.ClosePrice

    def OnStopped(self):
        self._previousCandleClose = None
        self._twoCandlesBackClose = None
        super(lunch_break_fade_strategy, self).OnStopped()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return lunch_break_fade_strategy()
