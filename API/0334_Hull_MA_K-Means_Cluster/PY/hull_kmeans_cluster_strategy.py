import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hull_kmeans_cluster_strategy(Strategy):
    """Strategy that trades based on Hull Moving Average direction with K-Means clustering for market state detection."""

    def __init__(self):
        super(hull_kmeans_cluster_strategy, self).__init__()

        # Strategy parameter: Hull Moving Average period.
        self._hull_period = self.Param("HullPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicator Settings")

        # Strategy parameter: Length of data to use for clustering.
        self._cluster_data_length = self.Param("ClusterDataLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Cluster Data Length", "Number of periods to use for clustering", "Clustering Settings")

        # Strategy parameter: RSI period for feature calculation.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation as a clustering feature", "Indicator Settings")

        # Strategy parameter: Candle type.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Enum representing market state
        class MarketState:
            Neutral = 0
            Bullish = 1
            Bearish = 2
        self._MarketState = MarketState

        # Feature data for clustering
        self._price_change_data = Queue[float]()
        self._rsi_data = Queue[float]()
        self._volume_ratio_data = Queue[float]()

        self._prev_hull_value = 0.0
        self._last_price = 0.0
        self._avg_volume = 0.0
        self._current_market_state = self._MarketState.Neutral

    @property
    def HullPeriod(self):
        return self._hull_period.Value

    @HullPeriod.setter
    def HullPeriod(self, value):
        self._hull_period.Value = value

    @property
    def ClusterDataLength(self):
        return self._cluster_data_length.Value

    @ClusterDataLength.setter
    def ClusterDataLength(self, value):
        self._cluster_data_length.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hull_kmeans_cluster_strategy, self).OnReseted()
        self._prev_hull_value = 0
        self._current_market_state = self._MarketState.Neutral
        self._last_price = 0
        self._avg_volume = 0
        self._price_change_data.Clear()
        self._rsi_data.Clear()
        self._volume_ratio_data.Clear()

    def OnStarted(self, time):
        super(hull_kmeans_cluster_strategy, self).OnStarted(time)



        # Create Hull Moving Average indicator
        hull_ma = HullMovingAverage()
        hull_ma.Length = self.HullPeriod

        # Create RSI indicator for feature calculation
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription and start
        subscription.Bind(hull_ma, rsi, self.ProcessCandle).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawOwnTrades(area)

        # Start position protection with ATR-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, hull_value, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update feature data for clustering
        self.UpdateFeatureData(candle, rsi_value)

        # Perform K-Means clustering when enough data is collected
        if (self._price_change_data.Count >= self.ClusterDataLength and
                self._rsi_data.Count >= self.ClusterDataLength and
                self._volume_ratio_data.Count >= self.ClusterDataLength):
            # Perform K-Means clustering for market state detection
            self._current_market_state = self.DetectMarketState()
            self.LogInfo("Current market state: {0}".format(self._current_market_state))

        # Check for Hull MA direction change
        is_hull_rising = hull_value > self._prev_hull_value

        # Trading logic based on Hull MA direction and market state
        if is_hull_rising and self._current_market_state == self._MarketState.Bullish and self.Position <= 0:
            # Hull MA rising in bullish market state - Buy signal
            self.LogInfo("Buy signal: Hull MA rising ({0} > {1}) in bullish market state".format(hull_value, self._prev_hull_value))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif not is_hull_rising and self._current_market_state == self._MarketState.Bearish and self.Position >= 0:
            # Hull MA falling in bearish market state - Sell signal
            self.LogInfo("Sell signal: Hull MA falling ({0} < {1}) in bearish market state".format(hull_value, self._prev_hull_value))
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Store Hull MA value for next comparison
        self._prev_hull_value = hull_value

        # Update last price
        self._last_price = float(candle.ClosePrice)

    def UpdateFeatureData(self, candle, rsi_value):
        # Calculate price change percentage
        if self._last_price != 0:
            price_change = float((candle.ClosePrice - self._last_price) / self._last_price * 100)

            # Maintain price change data queue
            self._price_change_data.Enqueue(price_change)
            if self._price_change_data.Count > self.ClusterDataLength:
                self._price_change_data.Dequeue()

        # Maintain RSI data queue
        self._rsi_data.Enqueue(rsi_value)
        if self._rsi_data.Count > self.ClusterDataLength:
            self._rsi_data.Dequeue()

        # Calculate volume ratio and maintain queue
        if self._avg_volume == 0:
            self._avg_volume = float(candle.TotalVolume)
        else:
            # Exponential smoothing for average volume
            self._avg_volume = float(0.9 * self._avg_volume + 0.1 * candle.TotalVolume)

        volume_ratio = candle.TotalVolume / (self._avg_volume if self._avg_volume != 0 else 1)
        self._volume_ratio_data.Enqueue(volume_ratio)
        if self._volume_ratio_data.Count > self.ClusterDataLength:
            self._volume_ratio_data.Dequeue()

    def DetectMarketState(self):
        # Simplified implementation of K-Means clustering for market state detection
        # This is a basic approach - a full implementation would use proper K-Means algorithm

        # Calculate feature averages to represent cluster centers
        avg_price_change = 0 if self._price_change_data.Count == 0 else sum(self._price_change_data) / float(self._price_change_data.Count)
        avg_rsi = 0 if self._rsi_data.Count == 0 else sum(self._rsi_data) / float(self._rsi_data.Count)
        avg_volume_ratio = 0 if self._volume_ratio_data.Count == 0 else sum(self._volume_ratio_data) / float(self._volume_ratio_data.Count)

        # Detect market state based on features
        # Higher RSI, positive price change and higher volume -> Bullish
        # Lower RSI, negative price change and higher volume -> Bearish
        # Otherwise -> Neutral

        if avg_rsi > 60 and avg_price_change > 0.1 and avg_volume_ratio > 1.1:
            return self._MarketState.Bullish
        elif avg_rsi < 40 and avg_price_change < -0.1 and avg_volume_ratio > 1.1:
            return self._MarketState.Bearish
        else:
            return self._MarketState.Neutral

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_kmeans_cluster_strategy()