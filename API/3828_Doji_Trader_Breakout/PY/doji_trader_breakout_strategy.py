import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class doji_trader_breakout_strategy(Strategy):
    def __init__(self):
        super(doji_trader_breakout_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Indicators")
        self._doji_ratio = self.Param("DojiRatio", 0.25) \
            .SetDisplay("Doji Ratio", "Max body/range ratio for doji detection", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def doji_ratio(self):
        return self._doji_ratio.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_trader_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._has_prev = False

    def OnStarted(self, time):
        super(doji_trader_breakout_strategy, self).OnStarted(time)
        self._has_prev = False
        self._prev_was_doji = False
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()

    def process_candle(self, candle, sma):
        if candle.State != CandleStates.Finished:
            return
        sma_val = float(sma)
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        is_doji = rng > 0 and body / rng < self.doji_ratio
        if self._has_prev and self._prev_was_doji:
            close = float(candle.ClosePrice)
            if close > self._prev_high and close > sma_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif close < self._prev_low and close < sma_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_was_doji = is_doji
        self._has_prev = True

    def CreateClone(self):
        return doji_trader_breakout_strategy()
