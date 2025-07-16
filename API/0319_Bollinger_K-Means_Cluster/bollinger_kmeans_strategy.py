import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from enum import Enum


class ClusterState(Enum):
    """Cluster state tracking."""
    Oversold = 0
    Neutral = 1
    Overbought = 2


class bollinger_kmeans_strategy(Strategy):
    """
    Bollinger Bands with K-Means clustering strategy.
    Uses Bollinger Bands indicator along with a simple K-Means clustering algorithm
    to identify overbought/oversold conditions.
    """

    def __init__(self):
        super(bollinger_kmeans_strategy, self).__init__()

        # Strategy parameters
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Length of the Bollinger Bands indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._kmeans_history_length = self.Param("KMeansHistoryLength", 50) \
            .SetDisplay("K-Means History Length", "Length of history for K-Means clustering", "Clustering") \
            .SetCanOptimize(True) \
            .SetOptimize(30, 100, 10)

        # Internal indicators and state
        self._bollinger = None
        self._rsi = None
        self._atr = None
        self._atr_value = 0.0

        self._current_cluster_state = ClusterState.Neutral
        self._rsi_values = []
        self._price_values = []
        self._volume_values = []

    @property
    def BollingerLength(self):
        """Bollinger Bands period."""
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands standard deviation multiplier."""
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def CandleType(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def KMeansHistoryLength(self):
        """Length of history for K-Means clustering."""
        return self._kmeans_history_length.Value

    @KMeansHistoryLength.setter
    def KMeansHistoryLength(self, value):
        self._kmeans_history_length.Value = value

    def OnStarted(self, time):
        """Initialize indicators, subscription and charting."""
        super(bollinger_kmeans_strategy, self).OnStarted(time)

        self._atr_value = 0.0
        self._current_cluster_state = ClusterState.Neutral
        self._rsi_values.clear()
        self._price_values.clear()
        self._volume_values.clear()

        # Create indicators
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BollingerLength
        self._bollinger.Width = self.BollingerDeviation

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create and initialize subscription
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._bollinger, self._rsi, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, bollinger_value, rsi_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bollinger_typed = bollinger_value

        # Extract values from indicators
        bollinger_upper = bollinger_typed.UpBand
        bollinger_middle = bollinger_typed.MovingAverage
        bollinger_lower = bollinger_typed.LowBand

        rsi = float(rsi_value)
        self._atr_value = float(atr_value)

        # Update data for clustering
        self.UpdateClusterData(candle, rsi)

        # Calculate K-Means clusters and determine market state
        self.CalculateClusters()

        # Trading logic
        if candle.ClosePrice < bollinger_lower and self._current_cluster_state == ClusterState.Oversold and self.Position <= 0:
            # Buy signal - price below lower band and in oversold cluster
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: Price below lower band ({0:F2}) in oversold cluster".format(bollinger_lower))
        elif candle.ClosePrice > bollinger_upper and self._current_cluster_state == ClusterState.Overbought and self.Position >= 0:
            # Sell signal - price above upper band and in overbought cluster
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Sell Signal: Price above upper band ({0:F2}) in overbought cluster".format(bollinger_upper))
        elif self.Position > 0 and candle.ClosePrice > bollinger_middle:
            # Exit long position when price returns to middle band
            self.SellMarket(self.Position)
            self.LogInfo("Exit Long: Price returned to middle band ({0:F2})".format(bollinger_middle))
        elif self.Position < 0 and candle.ClosePrice < bollinger_middle:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: Price returned to middle band ({0:F2})".format(bollinger_middle))

    def UpdateClusterData(self, candle, rsi):
        # Add current values to the data series
        self._price_values.append(candle.ClosePrice)
        self._rsi_values.append(rsi)
        self._volume_values.append(candle.TotalVolume)

        # Maintain the desired history length
        while len(self._price_values) > self.KMeansHistoryLength:
            self._price_values.pop(0)
            self._rsi_values.pop(0)
            self._volume_values.pop(0)

    def CalculateClusters(self):
        # Only perform clustering when we have enough data
        if len(self._price_values) < self.KMeansHistoryLength:
            return

        # Normalize the data (simple min-max normalization)
        normalized_rsi = self._rsi_values[-1] / 100.0  # RSI is already 0-100

        # Find min/max for price normalization
        min_price = min(self._price_values)
        max_price = max(self._price_values)

        # Normalize the last price
        normalized_price = 0.5
        if max_price != min_price:
            price_range = max_price - min_price
            normalized_price = (self._price_values[-1] - min_price) / price_range

        # Simple rules-based clustering (simplified K-means approximation)
        # Oversold: Low RSI (< 30) and price near bottom of range
        # Overbought: High RSI (> 70) and price near top of range
        # Neutral: Everything else
        if normalized_rsi < 0.3 and normalized_price < 0.3:
            self._current_cluster_state = ClusterState.Oversold
        elif normalized_rsi > 0.7 and normalized_price > 0.7:
            self._current_cluster_state = ClusterState.Overbought
        else:
            self._current_cluster_state = ClusterState.Neutral

        self.LogInfo(
            "Cluster State: {0}, Normalized RSI: {1:F2}, Normalized Price: {2:F2}".format(
                self._current_cluster_state.name, normalized_rsi, normalized_price))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_kmeans_strategy()
