import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_sample_strategy(Strategy):
    def __init__(self):
        super(macd_sample_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ma_trend_period = self.Param("MaTrendPeriod", 50) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._ema_value = 0.0
        self._has_ema = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_trend_period(self):
        return self._ma_trend_period.Value

    def OnReseted(self):
        super(macd_sample_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._ema_value = 0.0
        self._has_ema = False

    def OnStarted(self, time):
        super(macd_sample_strategy, self).OnStarted(time)
        macd_signal = MovingAverageConvergenceDivergenceSignal()
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_trend_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_ema).Start()
        subscription.BindEx(macd_signal, self.on_macd).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd_signal)
            self.DrawOwnTrades(area)

    def on_ema(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        self._ema_value = float(ema_val)
        self._has_ema = True

    def on_macd(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_ema:
            return
        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        if macd_line == 0 and signal_line == 0:
            return
        if not self._has_prev:
            self._prev_macd = macd_line
            self._prev_signal = signal_line
            self._has_prev = True
            return
        close = float(candle.ClosePrice)
        # Buy: MACD crosses above signal, price above EMA
        if self._prev_macd <= self._prev_signal and macd_line > signal_line and close > self._ema_value:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Sell: MACD crosses below signal, price below EMA
        elif self._prev_macd >= self._prev_signal and macd_line < signal_line and close < self._ema_value:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_macd = macd_line
        self._prev_signal = signal_line

    def CreateClone(self):
        return macd_sample_strategy()
