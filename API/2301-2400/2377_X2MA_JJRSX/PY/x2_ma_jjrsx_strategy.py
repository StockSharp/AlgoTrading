import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class x2_ma_jjrsx_strategy(Strategy):
    def __init__(self):
        super(x2_ma_jjrsx_strategy, self).__init__()
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Trend Candle Type", "Timeframe for trend moving averages", "General")
        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Signal Candle Type", "Timeframe for entry signals", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetDisplay("Fast MA Period", "Length of fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetDisplay("Slow MA Period", "Length of slow moving average", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of RSI filter", "Indicators")
        self._overbought = self.Param("Overbought", 75.0) \
            .SetDisplay("Overbought", "RSI overbought threshold", "Risk")
        self._oversold = self.Param("Oversold", 25.0) \
            .SetDisplay("Oversold", "RSI oversold threshold", "Risk")
        self._use_long = self.Param("UseLong", True) \
            .SetDisplay("Enable Long", "Allow long trades", "General")
        self._use_short = self.Param("UseShort", True) \
            .SetDisplay("Enable Short", "Allow short trades", "General")
        self._trend = 0
        self._prev_rsi = 50.0

    @property
    def trend_candle_type(self):
        return self._trend_candle_type.Value

    @property
    def signal_candle_type(self):
        return self._signal_candle_type.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def use_long(self):
        return self._use_long.Value

    @property
    def use_short(self):
        return self._use_short.Value

    def OnReseted(self):
        super(x2_ma_jjrsx_strategy, self).OnReseted()
        self._trend = 0
        self._prev_rsi = 50.0

    def OnStarted2(self, time):
        super(x2_ma_jjrsx_strategy, self).OnStarted2(time)
        self._trend = 0
        self._prev_rsi = 50.0
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = int(self.fast_ma_period)
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = int(self.slow_ma_period)
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self.rsi_period)
        trend_sub = self.SubscribeCandles(self.trend_candle_type)
        trend_sub.Bind(fast_ma, slow_ma, self._process_trend).Start()
        signal_sub = self.SubscribeCandles(self.signal_candle_type)
        signal_sub.Bind(rsi, self._process_signal).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, signal_sub)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _process_trend(self, candle, fast_ma, slow_ma):
        if candle.State != CandleStates.Finished:
            return
        fast_ma = float(fast_ma)
        slow_ma = float(slow_ma)
        if fast_ma > slow_ma:
            self._trend = 1
        elif fast_ma < slow_ma:
            self._trend = -1

    def _process_signal(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return
        rsi = float(rsi)
        ob = float(self.overbought)
        os_val = float(self.oversold)
        if self.use_long and self._trend > 0 and self.Position <= 0 and self._prev_rsi < os_val and rsi >= os_val:
            self.BuyMarket()
        if self.use_short and self._trend < 0 and self.Position >= 0 and self._prev_rsi > ob and rsi <= ob:
            self.SellMarket()
        if self.Position > 0 and (rsi >= ob or self._trend < 0):
            self.SellMarket()
        if self.Position < 0 and (rsi <= os_val or self._trend > 0):
            self.BuyMarket()
        self._prev_rsi = rsi

    def CreateClone(self):
        return x2_ma_jjrsx_strategy()
