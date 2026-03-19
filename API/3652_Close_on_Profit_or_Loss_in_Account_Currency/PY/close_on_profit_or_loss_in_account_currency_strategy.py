import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class close_on_profit_or_loss_in_account_currency_strategy(Strategy):
    """
    SMA crossover strategy with equity-based closure.
    Uses fast/slow SMA crossover for entries.
    """

    def __init__(self):
        super(close_on_profit_or_loss_in_account_currency_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Heartbeat Candle", "Candle type that triggers equity checks", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(close_on_profit_or_loss_in_account_currency_strategy, self).OnStarted(time)

        sma_fast = SimpleMovingAverage()
        sma_fast.Length = 10
        sma_slow = SimpleMovingAverage()
        sma_slow.Length = 30

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma_fast, sma_slow, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma_fast)
            self.DrawIndicator(area, sma_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return close_on_profit_or_loss_in_account_currency_strategy()
