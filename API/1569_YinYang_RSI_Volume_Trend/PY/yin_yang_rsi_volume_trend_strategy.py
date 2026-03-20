import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class yin_yang_rsi_volume_trend_strategy(Strategy):
    def __init__(self):
        super(yin_yang_rsi_volume_trend_strategy, self).__init__()
        self._trend_length = self.Param("TrendLength", 40) \
            .SetDisplay("Trend Length", "Lookback length", "General")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 0.5) \
            .SetDisplay("SL Mult %", "Stop distance percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_zone_high = 0.0
        self._prev_zone_low = 0.0
        self._prev_zone_basis = 0.0
        self._initialized = False

    @property
    def trend_length(self):
        return self._trend_length.Value

    @property
    def stop_loss_multiplier(self):
        return self._stop_loss_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(yin_yang_rsi_volume_trend_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_zone_high = 0.0
        self._prev_zone_low = 0.0
        self._prev_zone_basis = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(yin_yang_rsi_volume_trend_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.trend_length
        std_dev = StandardDeviation()
        std_dev.Length = self.trend_length
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if std_val <= 0:
            return
        # Dynamic zones based on SMA +/- RSI-weighted StdDev
        rsi_weight = rsi_val / 100; // 0..1
        zone_width = std_val * (0.5 + rsi_weight)
        zone_basis = sma_val
        zone_high = zone_basis + zone_width
        zone_low = zone_basis - zone_width
        stop_high = zone_high * (1 + self.stop_loss_multiplier / 100)
        stop_low = zone_low * (1 - self.stop_loss_multiplier / 100)
        close = candle.ClosePrice
        if not self._initialized:
            self._prev_close = close
            self._prev_zone_high = zone_high
            self._prev_zone_low = zone_low
            self._prev_zone_basis = zone_basis
            self._initialized = True
            return
        # Cross detections
        long_start = self._prev_close <= self._prev_zone_low and close > zone_low
        long_end = self._prev_close <= self._prev_zone_high and close > zone_high
        long_stop_loss = self._prev_close >= self._prev_zone_low and close < stop_low
        short_start = self._prev_close >= self._prev_zone_high and close < zone_high
        short_end = self._prev_close >= self._prev_zone_low and close < zone_low
        short_stop_loss = self._prev_close <= self._prev_zone_high and close > stop_high
        # Long entry: price crosses up from below lower zone
        if long_start and self.Position <= 0:
            self.BuyMarket()
        elif self.Position > 0 and (long_end or long_stop_loss):
            self.SellMarket()
        # Short entry: price crosses down from above upper zone
        if short_start and self.Position >= 0:
            self.SellMarket()
        elif self.Position < 0 and (short_end or short_stop_loss):
            self.BuyMarket()
        self._prev_close = close
        self._prev_zone_high = zone_high
        self._prev_zone_low = zone_low
        self._prev_zone_basis = zone_basis

    def CreateClone(self):
        return yin_yang_rsi_volume_trend_strategy()
