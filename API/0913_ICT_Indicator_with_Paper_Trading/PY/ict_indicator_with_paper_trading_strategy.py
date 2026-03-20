import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ict_indicator_with_paper_trading_strategy(Strategy):
    def __init__(self):
        super(ict_indicator_with_paper_trading_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._order_block_high = None
        self._order_block_low = None
        self._prev_order_block_high = None
        self._prev_order_block_low = None
        self._prev_high = None
        self._prev_prev_high = None
        self._prev_low = None
        self._prev_prev_low = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ict_indicator_with_paper_trading_strategy, self).OnReseted()
        self._order_block_high = None
        self._order_block_low = None
        self._prev_order_block_high = None
        self._prev_order_block_low = None
        self._prev_high = None
        self._prev_prev_high = None
        self._prev_low = None
        self._prev_prev_low = None
        self._prev_close = None

    def OnStarted(self, time):
        super(ict_indicator_with_paper_trading_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        if self._prev_high is not None and self._prev_prev_high is not None:
            if self._prev_high <= self._prev_prev_high and high > self._prev_high:
                self._order_block_high = high
        if self._prev_low is not None and self._prev_prev_low is not None:
            if self._prev_low <= self._prev_prev_low and low > self._prev_low:
                self._order_block_low = low
        buy_signal = (self._prev_close is not None and
                      self._prev_order_block_high is not None and
                      self._order_block_high is not None and
                      self._prev_close <= self._prev_order_block_high and
                      close > self._order_block_high)
        sell_signal = (self._prev_close is not None and
                       self._prev_order_block_low is not None and
                       self._order_block_low is not None and
                       self._prev_order_block_low <= self._prev_close and
                       self._order_block_low > close)
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position > 0:
            self.SellMarket()
        self._prev_prev_high = self._prev_high
        self._prev_high = high
        self._prev_prev_low = self._prev_low
        self._prev_low = low
        self._prev_order_block_high = self._order_block_high
        self._prev_order_block_low = self._order_block_low
        self._prev_close = close

    def CreateClone(self):
        return ict_indicator_with_paper_trading_strategy()
