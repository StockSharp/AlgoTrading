import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class basket_close_strategy(Strategy):
    def __init__(self):
        super(basket_close_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._entry_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ema_period(self):
        return self._ema_period.Value
    @ema_period.setter
    def ema_period(self, value):
        self._ema_period.Value = value

    def OnReseted(self):
        super(basket_close_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False

    def OnStarted(self, time):
        super(basket_close_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._has_prev_signal = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        is_bullish = close > ema_value

        if self._has_prev_signal and is_bullish != self._was_bullish:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = float(close)
            elif not is_bullish and self.Position >= 0:
                self.SellMarket()
                self._entry_price = float(close)

        self._was_bullish = is_bullish
        self._has_prev_signal = True

    def CreateClone(self):
        return basket_close_strategy()
