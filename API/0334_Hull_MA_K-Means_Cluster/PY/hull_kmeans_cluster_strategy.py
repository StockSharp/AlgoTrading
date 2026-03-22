import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


NEUTRAL = 0
BULLISH = 1
BEARISH = 2


class hull_kmeans_cluster_strategy(Strategy):
    """Strategy that trades based on Hull Moving Average direction with K-Means clustering for market state detection."""

    def __init__(self):
        super(hull_kmeans_cluster_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicator Settings")

        self._cluster_data_length = self.Param("ClusterDataLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Cluster Data Length", "Number of periods to use for clustering", "Clustering Settings")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation as a clustering feature", "Indicator Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._price_change_data = []
        self._rsi_data = []
        self._volume_ratio_data = []

        self._prev_hull_value = 0.0
        self._last_price = 0.0
        self._avg_volume = 0.0
        self._current_market_state = NEUTRAL

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(hull_kmeans_cluster_strategy, self).OnReseted()
        self._prev_hull_value = 0.0
        self._current_market_state = NEUTRAL
        self._last_price = 0.0
        self._avg_volume = 0.0
        self._price_change_data = []
        self._rsi_data = []
        self._volume_ratio_data = []

    def OnStarted(self, time):
        super(hull_kmeans_cluster_strategy, self).OnStarted(time)

        hull_ma = HullMovingAverage()
        hull_ma.Length = int(self._hull_period.Value)

        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hull_ma, rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, hull_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hull_val = float(hull_value)
        rsi_val = float(rsi_value)
        self._update_feature_data(candle, rsi_val)

        data_len = int(self._cluster_data_length.Value)
        if len(self._price_change_data) >= data_len and \
           len(self._rsi_data) >= data_len and \
           len(self._volume_ratio_data) >= data_len:
            self._current_market_state = self._detect_market_state()

        is_hull_rising = hull_val > self._prev_hull_value

        if is_hull_rising and self._current_market_state == BULLISH and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif not is_hull_rising and self._current_market_state == BEARISH and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._prev_hull_value = hull_val
        self._last_price = float(candle.ClosePrice)

    def _update_feature_data(self, candle, rsi_value):
        data_len = int(self._cluster_data_length.Value)
        close_price = float(candle.ClosePrice)

        if self._last_price != 0.0:
            price_change = (close_price - self._last_price) / self._last_price * 100.0
            self._price_change_data.append(price_change)
            while len(self._price_change_data) > data_len:
                self._price_change_data.pop(0)

        self._rsi_data.append(rsi_value)
        while len(self._rsi_data) > data_len:
            self._rsi_data.pop(0)

        total_volume = float(candle.TotalVolume)
        if self._avg_volume == 0.0:
            self._avg_volume = total_volume
        else:
            self._avg_volume = 0.9 * self._avg_volume + 0.1 * total_volume

        volume_ratio = total_volume / (self._avg_volume if self._avg_volume != 0.0 else 1.0)
        self._volume_ratio_data.append(volume_ratio)
        while len(self._volume_ratio_data) > data_len:
            self._volume_ratio_data.pop(0)

    def _detect_market_state(self):
        if len(self._price_change_data) == 0 or len(self._rsi_data) == 0 or len(self._volume_ratio_data) == 0:
            return NEUTRAL

        avg_price_change = sum(self._price_change_data) / len(self._price_change_data)
        avg_rsi = sum(self._rsi_data) / len(self._rsi_data)
        avg_volume_ratio = sum(self._volume_ratio_data) / len(self._volume_ratio_data)

        if avg_rsi > 60.0 and avg_price_change > 0.1 and avg_volume_ratio > 1.1:
            return BULLISH
        elif avg_rsi < 40.0 and avg_price_change < -0.1 and avg_volume_ratio > 1.1:
            return BEARISH
        else:
            return NEUTRAL

    def CreateClone(self):
        return hull_kmeans_cluster_strategy()
