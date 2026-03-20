import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class indices_tester_strategy(Strategy):
    def __init__(self):
        super(indices_tester_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._session_start = self.Param("SessionStart", TimeSpan(0, 0, 0))
        self._session_end = self.Param("SessionEnd", TimeSpan(23, 0, 0))
        self._close_time = self.Param("CloseTime", TimeSpan(23, 30, 0))
        self._daily_trade_limit = self.Param("DailyTradeLimit", 1)
        self._max_open_positions = self.Param("MaxOpenPositions", 1)

        self._current_day = None
        self._trades_opened_today = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SessionStart(self):
        return self._session_start.Value

    @SessionStart.setter
    def SessionStart(self, value):
        self._session_start.Value = value

    @property
    def SessionEnd(self):
        return self._session_end.Value

    @SessionEnd.setter
    def SessionEnd(self, value):
        self._session_end.Value = value

    @property
    def CloseTime(self):
        return self._close_time.Value

    @CloseTime.setter
    def CloseTime(self, value):
        self._close_time.Value = value

    @property
    def DailyTradeLimit(self):
        return self._daily_trade_limit.Value

    @DailyTradeLimit.setter
    def DailyTradeLimit(self, value):
        self._daily_trade_limit.Value = value

    @property
    def MaxOpenPositions(self):
        return self._max_open_positions.Value

    @MaxOpenPositions.setter
    def MaxOpenPositions(self, value):
        self._max_open_positions.Value = value

    def OnReseted(self):
        super(indices_tester_strategy, self).OnReseted()
        self._current_day = None
        self._trades_opened_today = 0

    def OnStarted(self, time):
        super(indices_tester_strategy, self).OnStarted(time)
        self._current_day = None
        self._trades_opened_today = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        candle_time = candle.CloseTime
        candle_date = candle_time.Date

        if self._current_day is None or self._current_day != candle_date:
            self._current_day = candle_date
            self._trades_opened_today = 0

        time_of_day = candle_time.TimeOfDay

        # Liquidate open positions once the configured close time is reached
        if self.Position > 0 and time_of_day >= self.CloseTime:
            self.SellMarket()
            return

        # Only evaluate entries strictly inside the trading window
        if time_of_day <= self.SessionStart or time_of_day >= self.SessionEnd:
            return

        # Respect the daily trade allowance
        if self._trades_opened_today >= self.DailyTradeLimit:
            return

        # Skip if already have max positions
        if self.Position > 0:
            return

        # Long-only: buy
        self.BuyMarket()
        self._trades_opened_today += 1

    def CreateClone(self):
        return indices_tester_strategy()
