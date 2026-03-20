import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_qqe_cloud_strategy(Strategy):
    def __init__(self):
        super(exp_qqe_cloud_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Length", "RSI period", "QQE")
        self._smooth_period = self.Param("SmoothPeriod", 5) \
            .SetDisplay("Smooth Period", "EMA smoothing period for RSI", "QQE")
        self._qqe_factor = self.Param("QqeFactor", 4.236) \
            .SetDisplay("QQE Factor", "QQE volatility factor", "QQE")
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def smooth_period(self):
        return self._smooth_period.Value

    @property
    def qqe_factor(self):
        return self._qqe_factor.Value

    def OnReseted(self):
        super(exp_qqe_cloud_strategy, self).OnReseted()
        self._bar_count = 0

    def OnStarted(self, time):
        super(exp_qqe_cloud_strategy, self).OnStarted(time)
        self._bar_count = 0
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self.rsi_period)
        ema = ExponentialMovingAverage()
        ema.Length = int(self.smooth_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        if self._bar_count < 3:
            return
        rsi_value = float(rsi_value)
        ema_value = float(ema_value)
        price = float(candle.ClosePrice)
        if rsi_value > 65 and price > ema_value and self.Position <= 0:
            self.BuyMarket()
        elif rsi_value < 35 and price < ema_value and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return exp_qqe_cloud_strategy()
