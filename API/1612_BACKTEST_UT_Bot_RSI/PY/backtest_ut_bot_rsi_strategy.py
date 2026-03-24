import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class backtest_ut_bot_rsi_strategy(Strategy):
    def __init__(self):
        super(backtest_ut_bot_rsi_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Parameters")
        self._std_length = self.Param("StdLength", 10) \
            .SetDisplay("StdDev Length", "StdDev period", "Parameters")
        self._factor = self.Param("Factor", 1.0) \
            .SetDisplay("UT Bot Factor", "Volatility multiplier", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._trail = None
        self._dir = 0
        self._prev_dir = 0

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def std_length(self):
        return self._std_length.Value

    @property
    def factor(self):
        return self._factor.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(backtest_ut_bot_rsi_strategy, self).OnReseted()
        self._trail = None
        self._dir = 0
        self._prev_dir = 0

    def OnStarted(self, time):
        super(backtest_ut_bot_rsi_strategy, self).OnStarted(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.std_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, std_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if std_value <= 0:
            return
        upper_band = candle.ClosePrice + self.factor * std_value
        lower_band = candle.ClosePrice - self.factor * std_value
        if self._trail is None:
            self._trail = lower_band
            self._dir = 0
        elif candle.ClosePrice > self._trail:
            self._trail = max(self._trail, lower_band)
            self._dir = 1
        elif candle.ClosePrice < self._trail:
            self._trail = min(self._trail, upper_band)
            self._dir = -1
        trend_up = self._dir == 1 and self._prev_dir == -1
        trend_down = self._dir == -1 and self._prev_dir == 1
        if trend_up and candle.ClosePrice < ema_value and self.Position <= 0:
            self.BuyMarket()
        elif trend_down and candle.ClosePrice > ema_value and self.Position >= 0:
            self.SellMarket()
        self._prev_dir = self._dir

    def CreateClone(self):
        return backtest_ut_bot_rsi_strategy()
