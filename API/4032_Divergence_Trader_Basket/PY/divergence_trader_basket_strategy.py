import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class divergence_trader_basket_strategy(Strategy):
    """
    Divergence-based strategy using fast and slow SMA.
    Trades based on the divergence crossing zero line between two moving averages.
    """

    def __init__(self):
        super(divergence_trader_basket_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Fast SMA Period", "Length of the fast simple moving average", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 88) \
            .SetDisplay("Slow SMA Period", "Length of the slow simple moving average", "Indicators")
        self._buy_threshold = self.Param("BuyThreshold", 0.0001) \
            .SetDisplay("Buy Threshold", "Minimum divergence value required before buying", "Signals")
        self._stay_out_threshold = self.Param("StayOutThreshold", 1000.0) \
            .SetDisplay("Stay-Out Threshold", "Upper divergence limit that disables new entries", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe used for calculations", "General")

        self._previous_difference = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(divergence_trader_basket_strategy, self).OnReseted()
        self._previous_difference = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(divergence_trader_basket_strategy, self).OnStarted(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self._fast_period.Value
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        current_diff = float(fast_value) - float(slow_value)

        if self._previous_difference is None:
            self._previous_difference = current_diff
            return

        prev_diff = self._previous_difference
        self._previous_difference = current_diff

        if self.Position != 0:
            if self.Position > 0 and current_diff < 0:
                self.SellMarket()
                self._entry_price = 0.0
            elif self.Position < 0 and current_diff > 0:
                self.BuyMarket()
                self._entry_price = 0.0
            return

        if current_diff > 0 and prev_diff <= 0:
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
        elif current_diff < 0 and prev_diff >= 0:
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

    def CreateClone(self):
        return divergence_trader_basket_strategy()
