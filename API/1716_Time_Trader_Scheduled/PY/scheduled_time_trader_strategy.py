import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class scheduled_time_trader_strategy(Strategy):
    def __init__(self):
        super(scheduled_time_trader_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(scheduled_time_trader_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(scheduled_time_trader_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            # Buy: close crosses above EMA and RSI confirms
        if self._prev_close <= self._prev_ema and close > ema_value and rsi_value < 65 and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Sell: close crosses below EMA and RSI confirms
        elif self._prev_close >= self._prev_ema and close < ema_value and rsi_value > 35 and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_ema = ema_value
        self._prev_close = close

    def CreateClone(self):
        return scheduled_time_trader_strategy()
