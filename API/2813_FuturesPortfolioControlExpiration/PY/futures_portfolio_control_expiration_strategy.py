import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class futures_portfolio_control_expiration_strategy(Strategy):
    """
    Futures Portfolio Control Expiration: rebalances to target position
    at regular intervals with SMA trend reversal exits.
    """

    def __init__(self):
        super(futures_portfolio_control_expiration_strategy, self).__init__()
        self._target_position = self.Param("TargetPosition", 1) \
            .SetDisplay("Target Position", "Desired position size (positive=long, negative=short)", "Portfolio")
        self._rebalance_period = self.Param("RebalancePeriod", 10) \
            .SetDisplay("Rebalance Period", "Number of bars between rebalance checks", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for monitoring", "General")

        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(futures_portfolio_control_expiration_strategy, self).OnReseted()
        self._bar_count = 0

    def OnStarted2(self, time):
        super(futures_portfolio_control_expiration_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        self._bar_count += 1

        price = float(candle.ClosePrice)
        sma = float(sma_value)
        target = self._target_position.Value

        # Rebalance at intervals
        if self._bar_count % self._rebalance_period.Value == 0:
            current = self.Position
            diff = target - current
            if diff > 0:
                self.BuyMarket()
            elif diff < 0:
                self.SellMarket()

        # Trend reversal exit
        if self.Position > 0 and price < sma:
            self.SellMarket()
        elif self.Position < 0 and price > sma:
            self.BuyMarket()
        elif self.Position == 0:
            if target > 0 and price > sma:
                self.BuyMarket()
            elif target < 0 and price < sma:
                self.SellMarket()
            elif target > 0:
                self.BuyMarket()
            elif target < 0:
                self.SellMarket()

    def CreateClone(self):
        return futures_portfolio_control_expiration_strategy()
