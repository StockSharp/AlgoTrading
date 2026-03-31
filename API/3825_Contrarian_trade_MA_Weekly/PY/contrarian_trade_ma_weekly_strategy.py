import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class contrarian_trade_ma_weekly_strategy(Strategy):
    def __init__(self):
        super(contrarian_trade_ma_weekly_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 14) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._channel_period = self.Param("ChannelPeriod", 10) \
            .SetDisplay("Channel Period", "Highest/Lowest lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(contrarian_trade_ma_weekly_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(contrarian_trade_ma_weekly_strategy, self).OnStarted2(time)
        self._has_prev = False
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, sma, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        sma_val = float(sma)
        high_val = float(highest)
        low_val = float(lowest)
        if not self._has_prev:
            self._prev_close = close
            self._prev_sma = sma_val
            self._has_prev = True
            return
        mid = (high_val + low_val) / 2.0
        if self._prev_close >= self._prev_sma and close < sma_val and close < mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_close <= self._prev_sma and close > sma_val and close > mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return contrarian_trade_ma_weekly_strategy()
