import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class iron_bot_statistical_trend_filter_strategy(Strategy):
    def __init__(self):
        super(iron_bot_statistical_trend_filter_strategy, self).__init__()
        self._z_length = self.Param("ZLength", 40) \
            .SetDisplay("Z Length", "Length for Z-score", "General")
        self._analysis_window = self.Param("AnalysisWindow", 44) \
            .SetDisplay("Analysis Window", "Lookback for trend", "General")
        self._high_trend_limit = self.Param("HighTrendLimit", 0.236) \
            .SetDisplay("Fibo High", "High trend Fibonacci", "General")
        self._low_trend_limit = self.Param("LowTrendLimit", 0.786) \
            .SetDisplay("Fibo Low", "Low trend Fibonacci", "General")
        self._sl_ratio = self.Param("SlRatio", 0.008) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp1_ratio = self.Param("Tp1Ratio", 0.0075) \
            .SetDisplay("TP1 %", "Take profit level", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._highest_high = 0.0
        self._lowest_low = 999999999.0
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iron_bot_statistical_trend_filter_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._highest_high = 0.0
        self._lowest_low = 999999999.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(iron_bot_statistical_trend_filter_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self._z_length.Value
        std = StandardDeviation()
        std.Length = self._z_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        if high > self._highest_high:
            self._highest_high = high
        if low < self._lowest_low:
            self._lowest_low = low
        aw = self._analysis_window.Value
        if self._bar_count < aw:
            return
        sma_v = float(sma_val)
        std_v = float(std_val)
        z_score = 0.0 if std_v == 0 else (close - sma_v) / std_v
        rng = self._highest_high - self._lowest_low
        if rng <= 0:
            return
        ht = float(self._high_trend_limit.Value)
        lt = float(self._low_trend_limit.Value)
        high_trend_level = self._highest_high - rng * ht
        trend_line = self._highest_high - rng * 0.5
        low_trend_level = self._highest_high - rng * lt
        sl = float(self._sl_ratio.Value)
        tp = float(self._tp1_ratio.Value)
        if self.Position > 0:
            pnl = (close - self._entry_price) / self._entry_price if self._entry_price > 0 else 0
            if pnl <= -sl or pnl >= tp:
                self.SellMarket()
                self._entry_price = 0.0
            return
        elif self.Position < 0:
            pnl = (self._entry_price - close) / self._entry_price if self._entry_price > 0 else 0
            if pnl <= -sl or pnl >= tp:
                self.BuyMarket()
                self._entry_price = 0.0
            return
        can_long = close >= trend_line and close >= high_trend_level and z_score >= 0
        can_short = close <= trend_line and close <= low_trend_level and z_score <= 0
        if can_long:
            self.BuyMarket()
            self._entry_price = close
        elif can_short:
            self.SellMarket()
            self._entry_price = close

    def CreateClone(self):
        return iron_bot_statistical_trend_filter_strategy()
