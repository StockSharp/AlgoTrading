import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class waddah_attar_trend_strategy(Strategy):
    MODE_DIRECT = 0
    MODE_REVERSE = 1

    def __init__(self):
        super(waddah_attar_trend_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA Length", "Fast EMA period for MACD", "Indicator")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA Length", "Slow EMA period for MACD", "Indicator")
        self._ma_length = self.Param("MaLength", 9) \
            .SetDisplay("MA Length", "Smoothing moving average period", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar offset for signal detection", "General")
        self._trend_mode = self.Param("TrendMode", 0) \
            .SetDisplay("Trend Mode", "0=Direct, 1=Reverse", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")
        self._prev_trend = 0.0
        self._colors = []
        self._buffer_index = 0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    @property
    def trend_mode(self):
        return self._trend_mode.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(waddah_attar_trend_strategy, self).OnReseted()
        self._prev_trend = 0.0
        self._colors = []
        self._buffer_index = 0

    def OnStarted2(self, time):
        super(waddah_attar_trend_strategy, self).OnStarted2(time)
        sb = int(self.signal_bar)
        self._colors = [0.0] * (sb + 2)
        self._buffer_index = 0
        self._prev_trend = 0.0
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = int(self.fast_length)
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = int(self.slow_length)
        ma = SimpleMovingAverage()
        ma.Length = int(self.ma_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, ma, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent))

    def process_candle(self, candle, fast, slow, ma_value):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        ma_value = float(ma_value)
        macd = fast - slow
        trend = macd * ma_value
        color = 0.0 if trend >= self._prev_trend else 1.0
        buf_len = len(self._colors)
        self._colors[self._buffer_index % buf_len] = color
        self._buffer_index += 1
        sb = int(self.signal_bar)
        if self._buffer_index <= sb + 1:
            self._prev_trend = trend
            return
        signal_index = (self._buffer_index - sb - 1) % buf_len
        prev_signal_index = (self._buffer_index - sb - 2) % buf_len
        signal_color = self._colors[signal_index]
        prev_signal_color = self._colors[prev_signal_index]
        tm = int(self.trend_mode)
        if tm == self.MODE_DIRECT:
            if prev_signal_color == 0.0 and signal_color > 0.0 and self.Position <= 0:
                self.BuyMarket()
            elif prev_signal_color == 1.0 and signal_color < 1.0 and self.Position >= 0:
                self.SellMarket()
        else:
            if prev_signal_color == 1.0 and signal_color < 1.0 and self.Position <= 0:
                self.BuyMarket()
            elif prev_signal_color == 0.0 and signal_color > 0.0 and self.Position >= 0:
                self.SellMarket()
        self._prev_trend = trend

    def CreateClone(self):
        return waddah_attar_trend_strategy()
