import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class consecutive_bearish_candle_strategy(Strategy):
    """
    Consecutive Bearish Candle strategy.
    A candle is bearish when it closes below the previous close. After Lookback bearish candles in a row inside the
    StartTime-EndTime window the strategy buys, and it closes the long when a candle closes above the previous candle's high.
    """

    def __init__(self):
        super(consecutive_bearish_candle_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 3).SetGreaterThanZero().SetDisplay("Lookback", "Number of consecutive bearish candles", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_time = self.Param("StartTime", DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Time", "Start of the trading window", "Time")
        self._end_time = self.Param("EndTime", DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("End Time", "End of the trading window", "Time")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_high = None
        self._bearish_count = 0

    def OnReseted(self):
        super(consecutive_bearish_candle_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(consecutive_bearish_candle_strategy, self).OnStarted2(time)

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

        last_close = self._prev_close
        last_high = self._prev_high
        self._prev_close = candle.ClosePrice
        self._prev_high = candle.HighPrice

        if last_close is None or last_high is None:
            return

        self._bearish_count = self._bearish_count + 1 if candle.ClosePrice < last_close else 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and candle.ClosePrice > last_high:
            self.SellMarket(self.Position)
            return

        in_window = self._start_time.Value.UtcDateTime <= candle.OpenTime <= self._end_time.Value.UtcDateTime

        if in_window and self._bearish_count >= self._lookback.Value and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return consecutive_bearish_candle_strategy()
