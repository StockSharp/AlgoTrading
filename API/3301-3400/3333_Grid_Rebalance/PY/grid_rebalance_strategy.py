import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class grid_rebalance_strategy(Strategy):
    """
    Grid Rebalance: RSI + EMA crossover strategy.
    Buys when close crosses above EMA with RSI < 55.
    Sells when close crosses below EMA with RSI > 45.
    """

    def __init__(self):
        super(grid_rebalance_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = None
        self._prev_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_rebalance_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ema = None

    def OnStarted2(self, time):
        super(grid_rebalance_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_val)
        ema = float(ema_val)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_up = self._prev_close <= self._prev_ema and close > ema
            cross_down = self._prev_close >= self._prev_ema and close < ema

            if cross_up and rsi < 55.0 and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and rsi > 45.0 and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema

    def CreateClone(self):
        return grid_rebalance_strategy()
