import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

# EntryOption values, matching the C# EntryOptions enum.
ENTRY_LONG_AT_HIGH = 0
ENTRY_SHORT_AT_HIGH = 1
ENTRY_LONG_AT_LOW = 2
ENTRY_SHORT_AT_LOW = 3

# TimeframeOption values, matching the C# TimeframeOptions enum.
TIMEFRAME_DAILY = 0
TIMEFRAME_WEEKLY = 1
TIMEFRAME_MONTHLY = 2

class high_low_breakout_statistical_analysis_strategy(Strategy):
    """
    High Low Breakout Statistical Analysis strategy.
    Tracks the high and low of the previous completed day, week or month (TimeframeOption). Depending on EntryOption it buys or
    sells when the close crosses above the previous high, or buys or sells when the close crosses below the previous low. The
    position is closed after HoldingPeriod candles.
    """

    def __init__(self):
        super(high_low_breakout_statistical_analysis_strategy, self).__init__()
        self._entry_option = self.Param("EntryOption", ENTRY_LONG_AT_HIGH).SetDisplay("Entry Option", "0 LongAtHigh, 1 ShortAtHigh, 2 LongAtLow, 3 ShortAtLow", "Trading")
        self._timeframe_option = self.Param("TimeframeOption", TIMEFRAME_DAILY).SetDisplay("Timeframe Option", "0 Daily, 1 Weekly, 2 Monthly", "Trading")
        self._holding_period = self.Param("HoldingPeriod", 5).SetGreaterThanZero().SetDisplay("Holding Period", "Candles to hold a position", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._period_start = None
        self._period_high = 0.0
        self._period_low = 0.0
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._bars_in_position = 0

    def OnReseted(self):
        super(high_low_breakout_statistical_analysis_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(high_low_breakout_statistical_analysis_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_period_start(self, time):
        date = time.Date
        option = int(self._timeframe_option.Value)
        if option == TIMEFRAME_WEEKLY:
            return date.AddDays(-((int(date.DayOfWeek) + 6) % 7))
        if option == TIMEFRAME_MONTHLY:
            return date.AddDays(1 - date.Day)
        return date

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        period_start = self._get_period_start(candle.OpenTime)

        if self._period_start is None or self._period_start != period_start:
            if self._period_start is not None:
                self._prev_high = self._period_high
                self._prev_low = self._period_low
            self._period_start = period_start
            self._period_high = high_price
            self._period_low = low_price
        else:
            self._period_high = max(self._period_high, high_price)
            self._period_low = min(self._period_low, low_price)

        close = float(candle.ClosePrice)
        prev_close = self._prev_close
        self._prev_close = close

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= self._holding_period.Value:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return

        if prev_close is None or self._prev_high is None or self._prev_low is None:
            return

        cross_high = prev_close <= self._prev_high and close > self._prev_high
        cross_low = prev_close >= self._prev_low and close < self._prev_low
        option = int(self._entry_option.Value)

        if (option == ENTRY_LONG_AT_HIGH and cross_high) or (option == ENTRY_LONG_AT_LOW and cross_low):
            self.BuyMarket(self.Volume)
            self._bars_in_position = 0
        elif (option == ENTRY_SHORT_AT_HIGH and cross_high) or (option == ENTRY_SHORT_AT_LOW and cross_low):
            self.SellMarket(self.Volume)
            self._bars_in_position = 0

    def CreateClone(self):
        return high_low_breakout_statistical_analysis_strategy()
