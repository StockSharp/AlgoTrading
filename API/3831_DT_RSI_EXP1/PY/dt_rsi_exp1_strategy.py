import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class dt_rsi_exp1_strategy(Strategy):
    def __init__(self):
        super(dt_rsi_exp1_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dt_rsi_exp1_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(dt_rsi_exp1_strategy, self).OnStarted2(time)
        self._has_prev = False
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def process_candle(self, candle, rsi, ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        rsi_val = float(rsi)
        ema_val = float(ema)
        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return
        if self._prev_rsi <= self.oversold and rsi_val > self.oversold and close > ema_val and self.Position == 0:
            self.BuyMarket()
        elif self._prev_rsi >= self.overbought and rsi_val < self.overbought and close < ema_val and self.Position == 0:
            self.SellMarket()
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return dt_rsi_exp1_strategy()
