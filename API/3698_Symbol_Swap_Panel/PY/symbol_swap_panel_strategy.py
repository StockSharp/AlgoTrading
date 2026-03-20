import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class symbol_swap_panel_strategy(Strategy):

    def __init__(self):
        super(symbol_swap_panel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle series for monitoring and signals", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period for entry signals", "Indicators")

        self._entry_price = 0.0
        self._prev_close = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(symbol_swap_panel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0

    def OnStarted(self, time):
        super(symbol_swap_panel_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(sma, self._process_candle) \
            .Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        sma_v = float(sma_value)

        # Exit: reversal against trend
        if self.Position != 0 and self._entry_price > 0:
            if (self.Position > 0 and price < sma_v) or \
               (self.Position < 0 and price > sma_v):
                if self.Position > 0:
                    self.SellMarket(abs(self.Position))
                else:
                    self.BuyMarket(abs(self.Position))
                self._entry_price = 0.0
                self._prev_close = price
                return

        # Entry: follow MA trend with momentum confirmation
        if self.Position == 0 and self._prev_close > 0:
            if price > sma_v and self._prev_close <= sma_v:
                self.BuyMarket()
                self._entry_price = price
            elif price < sma_v and self._prev_close >= sma_v:
                self.SellMarket()
                self._entry_price = price

        self._prev_close = price

    def CreateClone(self):
        return symbol_swap_panel_strategy()
