import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class liquidity_breakout_strategy(Strategy):
    def __init__(self):
        super(liquidity_breakout_strategy, self).__init__()
        self._pivot_length = self.Param("PivotLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Bars for range", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(liquidity_breakout_strategy, self).OnStarted(time)
        ema_short = ExponentialMovingAverage()
        ema_short.Length = 50
        ema_long = ExponentialMovingAverage()
        ema_long.Length = 200
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_short, ema_long, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_short_val, ema_long_val):
        if candle.State != CandleStates.Finished:
            return
        sv = float(ema_short_val)
        lv = float(ema_long_val)
        if sv <= 0.0 or lv <= 0.0:
            return
        if sv > lv and self.Position <= 0:
            self.BuyMarket()
        elif sv < lv and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return liquidity_breakout_strategy()
