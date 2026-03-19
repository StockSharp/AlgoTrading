import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class maybeawo222_strategy(Strategy):
    """
    EMA crossover: buy when price crosses MA from below, sell from above.
    """

    def __init__(self):
        super(maybeawo222_strategy, self).__init__()
        self._moving_period = self.Param("MovingPeriod", 20).SetDisplay("MA Period", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_close = None
        self._prev_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(maybeawo222_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ma = None

    def OnStarted(self, time):
        super(maybeawo222_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._moving_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        ma = float(ma_val)
        if self._prev_close is None or self._prev_ma is None:
            self._prev_close = close
            self._prev_ma = ma
            return
        buy_signal = self._prev_close <= self._prev_ma and close > ma
        sell_signal = self._prev_close >= self._prev_ma and close < ma
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_close = close
        self._prev_ma = ma

    def CreateClone(self):
        return maybeawo222_strategy()
