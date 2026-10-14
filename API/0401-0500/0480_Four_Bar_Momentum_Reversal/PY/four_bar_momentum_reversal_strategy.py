import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class four_bar_momentum_reversal_strategy(Strategy):
    """
    Four Bar Momentum Reversal strategy.
    Counts consecutive candles whose close is below the close from Lookback bars ago. Once the count reaches BuyThreshold inside the
    StartTime..EndTime window the strategy buys, and it closes the long when the close breaks above the previous candle high.
    """

    def __init__(self):
        super(four_bar_momentum_reversal_strategy, self).__init__()
        self._buy_threshold = self.Param("BuyThreshold", 4).SetGreaterThanZero().SetDisplay("Buy Threshold", "Consecutive closes below the reference close required to buy", "Strategy")
        self._lookback = self.Param("Lookback", 4).SetGreaterThanZero().SetDisplay("Lookback", "How many bars back the reference close is taken", "Strategy")
        self._start_time = self.Param("StartTime", DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Time", "Beginning of the trading window", "General")
        self._end_time = self.Param("EndTime", DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("End Time", "End of the trading window", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._below_count = 0
        self._prev_high = None

    def OnReseted(self):
        super(four_bar_momentum_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(four_bar_momentum_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._lookback.Value
        close = candle.ClosePrice
        prev_high = self._prev_high
        self._prev_high = candle.HighPrice

        # _closes holds the previous Lookback closes, so its first element is the close from Lookback bars ago.
        ready = len(self._closes) >= lookback
        if ready:
            self._below_count = self._below_count + 1 if close < self._closes[0] else 0

        self._closes.append(close)
        while len(self._closes) > lookback:
            self._closes.pop(0)

        if not ready or not self.IsFormedAndOnlineAndAllowTrading():
            return

        in_window = self._start_time.Value.UtcDateTime <= candle.OpenTime <= self._end_time.Value.UtcDateTime

        if self.Position == 0 and in_window and self._below_count >= self._buy_threshold.Value:
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and prev_high is not None and close > prev_high:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return four_bar_momentum_reversal_strategy()
