import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class bulls_bears_power_average_strategy(Strategy):
    """
    Bulls Bears Power Average strategy. Uses EMA with bulls/bears power crossover.
    """

    def __init__(self):
        super(bulls_bears_power_average_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA lookback for power calc", "Indicators")

        self._prev_power = None

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
        super(bulls_bears_power_average_strategy, self).OnReseted()
        self._prev_power = None

    def OnStarted2(self, time):
        super(bulls_bears_power_average_strategy, self).OnStarted2(time)

        self._prev_power = None
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        bulls_power = candle.HighPrice - ema_val
        bears_power = candle.LowPrice - ema_val
        avg_power = (bulls_power + bears_power) / 2.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_power = avg_power
            return

        if self._prev_power is None:
            self._prev_power = avg_power
            return

        if self._prev_power < 0.0 and avg_power >= 0.0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_power > 0.0 and avg_power <= 0.0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_power = avg_power

    def CreateClone(self):
        return bulls_bears_power_average_strategy()
