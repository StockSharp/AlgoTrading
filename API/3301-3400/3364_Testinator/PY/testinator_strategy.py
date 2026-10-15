import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class testinator_strategy(Strategy):
    def __init__(self):
        super(testinator_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 55.0) \
            .SetDisplay("RSI Buy Level", "RSI threshold for buy signal", "Signals")
        self._rsi_sell_level = self.Param("RsiSellLevel", 45.0) \
            .SetDisplay("RSI Sell Level", "RSI threshold for sell signal", "Signals")

        self._rsi = None
        self._ema = None
        self._prev_close = None
        self._prev_ema = None

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def rsi_buy_level(self):
        return self._rsi_buy_level.Value

    @property
    def rsi_sell_level(self):
        return self._rsi_sell_level.Value

    def OnReseted(self):
        super(testinator_strategy, self).OnReseted()
        self._rsi = None
        self._ema = None
        self._prev_close = None
        self._prev_ema = None

    def OnStarted2(self, time):
        super(testinator_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._rsi, self._ema, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._ema.IsFormed:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        ema_val = float(ema_value)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_up = self._prev_close <= self._prev_ema and close > ema_val
            cross_down = self._prev_close >= self._prev_ema and close < ema_val

            if cross_up and rsi_val > self.rsi_buy_level and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and rsi_val < self.rsi_sell_level and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return testinator_strategy()
