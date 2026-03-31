import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class stat_euclidean_metric_strategy(Strategy):
    """MACD reversal with trend MA filter."""
    def __init__(self):
        super(stat_euclidean_metric_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast Length", "Fast EMA for MACD", "Indicators")
        self._slow_length = self.Param("SlowLength", 26).SetDisplay("Slow Length", "Slow EMA for MACD", "Indicators")
        self._trend_ma_length = self.Param("TrendMaLength", 50).SetDisplay("Trend MA Length", "Period for trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(stat_euclidean_metric_strategy, self).OnReseted()
        self._macd_history = []

    def OnStarted2(self, time):
        super(stat_euclidean_metric_strategy, self).OnStarted2(time)
        self._macd_history = []

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value
        trend_ma = SimpleMovingAverage()
        trend_ma.Length = self._trend_ma_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, trend_ma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, trend_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, trend_val):
        if candle.State != CandleStates.Finished:
            return

        macd_line = fast_val - slow_val
        self._macd_history.append(macd_line)
        if len(self._macd_history) > 5:
            self._macd_history.pop(0)

        if len(self._macd_history) < 3:
            return

        close = float(candle.ClosePrice)
        m1 = self._macd_history[-1]
        m2 = self._macd_history[-2]
        m3 = self._macd_history[-3]

        buy_reversal = m3 >= m2 and m2 < m1
        sell_reversal = m3 <= m2 and m2 > m1

        # Exits
        if self.Position > 0 and sell_reversal:
            self.SellMarket()
        elif self.Position < 0 and buy_reversal:
            self.BuyMarket()

        # Entries
        if self.Position == 0:
            if buy_reversal and close > trend_val:
                self.BuyMarket()
            elif sell_reversal and close < trend_val:
                self.SellMarket()

    def CreateClone(self):
        return stat_euclidean_metric_strategy()
