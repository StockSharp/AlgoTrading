import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class on_tick_market_watch_strategy(Strategy):
    def __init__(self):
        super(on_tick_market_watch_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnStarted2(self, time):
        super(on_tick_market_watch_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ev = float(ema_value)
        if close > ev and self.Position <= 0:
            self.BuyMarket()
        elif close < ev and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return on_tick_market_watch_strategy()
