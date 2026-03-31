import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_digital_macd_strategy(Strategy):
    def __init__(self):
        super(exp_digital_macd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA for MACD", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA for MACD", "Indicators")

        self._prev_macd = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(exp_digital_macd_strategy, self).OnReseted()
        self._prev_macd = None

    def OnStarted2(self, time):
        super(exp_digital_macd_strategy, self).OnStarted2(time)
        self._prev_macd = None

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)
        macd = fv - sv

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd = macd
            return

        if self._prev_macd is None:
            self._prev_macd = macd
            return

        # MACD crosses above zero
        if self._prev_macd <= 0 and macd > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # MACD crosses below zero
        elif self._prev_macd >= 0 and macd < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_macd = macd

    def CreateClone(self):
        return exp_digital_macd_strategy()
