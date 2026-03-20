import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class tokyo_session_strategy(Strategy):
    def __init__(self):
        super(tokyo_session_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators")

        self._atr = None
        self._session_high = None
        self._session_low = None
        self._candle_count = 0
        self._range_set = False
        self._prev_close = None

    @property
    def atr_period(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(tokyo_session_strategy, self).OnReseted()
        self._atr = None
        self._session_high = None
        self._session_low = None
        self._candle_count = 0
        self._range_set = False
        self._prev_close = None

    def OnStarted(self, time):
        super(tokyo_session_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._session_high = -999999999.0
        self._session_low = 999999999.0
        self._candle_count = 0
        self._range_set = False

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed:
            return

        self._candle_count += 1
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._candle_count <= 4:
            if high > self._session_high:
                self._session_high = high
            if low < self._session_low:
                self._session_low = low
            if self._candle_count == 4:
                self._range_set = True
            self._prev_close = close
            return

        if not self._range_set:
            self._prev_close = close
            return

        if self._candle_count % 48 == 0:
            self._session_high = high
            self._session_low = low
            self._range_set = False
            self._candle_count = 0
            self._prev_close = close
            return

        if self._prev_close is not None:
            cross_above = self._prev_close <= self._session_high and close > self._session_high
            cross_below = self._prev_close >= self._session_low and close < self._session_low

            if cross_above and self.Position <= 0:
                self.BuyMarket()
            elif cross_below and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close

    def CreateClone(self):
        return tokyo_session_strategy()
