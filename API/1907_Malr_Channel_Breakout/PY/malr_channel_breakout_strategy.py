import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, WeightedMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class malr_channel_breakout_strategy(Strategy):
    def __init__(self):
        super(malr_channel_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("MA", "Moving average period", "General") \
            .SetOptimize(50, 200, 10)
        self._channel_reversal = self.Param("ChannelReversal", 1.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Reversal", "Channel reversal width", "General") \
            .SetOptimize(0.5, 2.0, 0.1)
        self._channel_breakout = self.Param("ChannelBreakout", 1.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout", "Channel breakout width", "General") \
            .SetOptimize(0.5, 2.0, 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle", "Candle type", "General")
        self._sma = None
        self._lwma = None
        self._std_dev = None
        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None

    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def channel_reversal(self):
        return self._channel_reversal.Value
    @property
    def channel_breakout(self):
        return self._channel_breakout.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(malr_channel_breakout_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None

    def OnStarted(self, time):
        super(malr_channel_breakout_strategy, self).OnStarted(time)
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.ma_period
        self._lwma = WeightedMovingAverage()
        self._lwma.Length = self.ma_period
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._lwma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        sma_result = self._sma.Process(candle.ClosePrice, candle.OpenTime, True)
        lwma_result = self._lwma.Process(candle.ClosePrice, candle.OpenTime, True)
        if not sma_result.IsFormed or not lwma_result.IsFormed:
            self._prev_close = close
            return
        sma_val = float(sma_result.ToDecimal())
        lwma_val = float(lwma_result.ToDecimal())
        ff = 3.0 * lwma_val - 2.0 * sma_val
        deviation = close - ff
        std_result = self._std_dev.Process(deviation, candle.OpenTime, True)
        if not std_result.IsFormed:
            self._prev_close = close
            self._prev_upper = ff
            self._prev_lower = ff
            return
        std = float(std_result.ToDecimal())
        cr = float(self.channel_reversal)
        cb = float(self.channel_breakout)
        upper = ff + std * (cr + cb)
        lower = ff - std * (cr + cb)
        if self._prev_upper is not None and self._prev_lower is not None and self._prev_close is not None:
            if self._prev_close <= self._prev_upper and close > upper and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_close >= self._prev_lower and close < lower and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = close

    def CreateClone(self):
        return malr_channel_breakout_strategy()
