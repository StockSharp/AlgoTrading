import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class auto_trading_publish_strategy(Strategy):
    """
    Auto Trading Publish strategy: SMA crossover + RSI confirmation.
    Buys when close crosses above SMA and RSI is below 55.
    Sells when close crosses below SMA and RSI is above 45.
    """

    def __init__(self):
        super(auto_trading_publish_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI period", "Indicators")

        self._prev_close = None
        self._prev_sma = None

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def SmaPeriod(self): return self._sma_period.Value
    @SmaPeriod.setter
    def SmaPeriod(self, v): self._sma_period.Value = v
    @property
    def RsiPeriod(self): return self._rsi_period.Value
    @RsiPeriod.setter
    def RsiPeriod(self, v): self._rsi_period.Value = v

    def OnReseted(self):
        super(auto_trading_publish_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_sma = None

    def OnStarted2(self, time):
        super(auto_trading_publish_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_sma = None

        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close is not None and self._prev_sma is not None:
            cross_up = self._prev_close <= self._prev_sma and close > sma_val
            cross_down = self._prev_close >= self._prev_sma and close < sma_val

            if cross_up and rsi_val < 55 and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and rsi_val > 45 and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return auto_trading_publish_strategy()
