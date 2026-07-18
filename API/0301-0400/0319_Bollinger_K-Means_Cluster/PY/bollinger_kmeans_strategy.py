import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_kmeans_strategy(Strategy):
    OVERSOLD = 0
    NEUTRAL = 1
    OVERBOUGHT = 2

    def __init__(self):
        super(bollinger_kmeans_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Length of the Bollinger Bands indicator", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._kmeans_history_length = self.Param("KMeansHistoryLength", 50) \
            .SetDisplay("K-Means History Length", "Length of history for K-Means clustering", "Clustering")

        self._atr_value = 0.0
        self._current_cluster_state = bollinger_kmeans_strategy.NEUTRAL
        self._rsi_values = []
        self._price_values = []
        self._volume_values = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_kmeans_strategy, self).OnReseted()
        self._atr_value = 0.0
        self._current_cluster_state = bollinger_kmeans_strategy.NEUTRAL
        self._rsi_values = []
        self._price_values = []
        self._volume_values = []

    def OnStarted2(self, time):
        super(bollinger_kmeans_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = int(self._bollinger_length.Value)
        bollinger.Width = Decimal(self._bollinger_deviation.Value)

        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        atr = AverageTrueRange()
        atr.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def _process_candle(self, candle, bollinger_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        up_band = bollinger_value.UpBand
        low_band = bollinger_value.LowBand
        moving_avg = bollinger_value.MovingAverage

        if up_band is None or low_band is None or moving_avg is None:
            return

        upper = float(up_band)
        middle = float(moving_avg)
        lower = float(low_band)
        rsi = float(rsi_value)
        self._atr_value = float(atr_value)

        self._update_cluster_data(candle, rsi)
        self._calculate_clusters()

        price_step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            price_step = float(self.Security.PriceStep)
        band_buffer = max(self._atr_value * 0.1, price_step)

        close_price = float(candle.ClosePrice)

        if close_price < lower - band_buffer and self._current_cluster_state == bollinger_kmeans_strategy.OVERSOLD and self.Position <= 0:
            self.BuyMarket(self.Volume)
        elif close_price > upper + band_buffer and self._current_cluster_state == bollinger_kmeans_strategy.OVERBOUGHT and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and close_price > middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close_price < middle:
            self.BuyMarket(Math.Abs(self.Position))

    def _update_cluster_data(self, candle, rsi):
        kmeans_len = int(self._kmeans_history_length.Value)
        self._price_values.append(float(candle.ClosePrice))
        self._rsi_values.append(rsi)
        self._volume_values.append(float(candle.TotalVolume))
        while len(self._price_values) > kmeans_len:
            self._price_values.pop(0)
            self._rsi_values.pop(0)
            self._volume_values.pop(0)

    def _calculate_clusters(self):
        kmeans_len = int(self._kmeans_history_length.Value)
        if len(self._price_values) < kmeans_len:
            return

        normalized_rsi = self._rsi_values[-1] / 100.0
        min_price = min(self._price_values)
        max_price = max(self._price_values)
        normalized_price = 0.5
        if max_price != min_price:
            normalized_price = (self._price_values[-1] - min_price) / (max_price - min_price)

        if normalized_rsi < 0.3 and normalized_price < 0.3:
            self._current_cluster_state = bollinger_kmeans_strategy.OVERSOLD
        elif normalized_rsi > 0.7 and normalized_price > 0.7:
            self._current_cluster_state = bollinger_kmeans_strategy.OVERBOUGHT
        else:
            self._current_cluster_state = bollinger_kmeans_strategy.NEUTRAL

    def CreateClone(self):
        return bollinger_kmeans_strategy()
