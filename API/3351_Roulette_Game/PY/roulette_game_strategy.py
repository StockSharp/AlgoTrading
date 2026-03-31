import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class roulette_game_strategy(Strategy):
    def __init__(self):
        super(roulette_game_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero().SetDisplay("SMA Period", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(roulette_game_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_sma = 0
        self._has_prev = False

    def OnStarted2(self, time):
        super(roulette_game_strategy, self).OnStarted2(time)
        self._prev_close = 0
        self._prev_sma = 0
        self._has_prev = False

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        is_bullish = close > candle.OpenPrice

        if self._has_prev:
            cross_up = self._prev_close <= self._prev_sma and close > sma_val
            cross_down = self._prev_close >= self._prev_sma and close < sma_val

            if is_bullish and cross_up and self.Position <= 0:
                self.BuyMarket()
            elif not is_bullish and cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_sma = sma_val
        self._has_prev = True

    def CreateClone(self):
        return roulette_game_strategy()
