import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class multi_currency_template_mt5_strategy(Strategy):
    def __init__(self):
        super(multi_currency_template_mt5_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))

        self._prev_open = None
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_currency_template_mt5_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None

    def OnStarted2(self, time):
        super(multi_currency_template_mt5_strategy, self).OnStarted2(time)
        self._prev_open = None
        self._prev_close = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if self._prev_open is None or self._prev_close is None:
            self._prev_open = open_price
            self._prev_close = close
            return

        min_body = self._prev_open * 0.001

        # Buy: previous candle was bullish (close > open), current closes below previous open
        buy_signal = (close < self._prev_open - min_body and
                      self._prev_close > self._prev_open + min_body)
        # Sell: previous candle was bearish (close < open), current closes above previous open
        sell_signal = (close > self._prev_open + min_body and
                       self._prev_close < self._prev_open - min_body)

        if buy_signal:
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_open = open_price
        self._prev_close = close

    def CreateClone(self):
        return multi_currency_template_mt5_strategy()
