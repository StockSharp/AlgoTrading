import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class xau_usd_adx_bollinger_strategy(Strategy):
    def __init__(self):
        super(xau_usd_adx_bollinger_strategy, self).__init__()
        self._boll_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "SMA/StdDev period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2) \
            .SetDisplay("BB Width", "Band multiplier", "Indicators")
        self._trend_length = self.Param("TrendLength", 14) \
            .SetDisplay("Trend Length", "Directional movement lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def boll_period(self):
        return self._boll_period.Value

    @property
    def bb_width(self):
        return self._bb_width.Value

    @property
    def trend_length(self):
        return self._trend_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xau_usd_adx_bollinger_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(xau_usd_adx_bollinger_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.bollinger_period
        std_dev = StandardDeviation()
        std_dev.Length = self.bollinger_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        self._closes.append(candle.ClosePrice)
        while (len(self._closes) > self.trend_length + 1)
        self._closes.pop(0)
        if std_val <= 0 or len(self._closes) < self.trend_length:
            return
        upper = sma_val + self.bb_width * std_val
        lower = sma_val - self.bb_width * std_val
        # Simple trend strength: absolute price change over period / average range
        price_change = abs(candle.ClosePrice - self._closes[0])
        avg_change = std_val * 2; // approximate
        trend_strength = (price_change / avg_change if avg_change > 0 else 0)
        # Only trade when trend is reasonably strong
        if trend_strength > 1:
            if candle.ClosePrice > upper and self.Position <= 0:
                self.BuyMarket()
            elif candle.ClosePrice < lower and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return xau_usd_adx_bollinger_strategy()
