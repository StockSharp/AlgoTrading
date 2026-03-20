import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class she_kanskigor_daily_strategy(Strategy):
    def __init__(self):
        super(she_kanskigor_daily_strategy, self).__init__()

        self._take_profit_steps = self.Param("TakeProfitSteps", 35) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._stop_loss_steps = self.Param("StopLossSteps", 55) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._start_time = self.Param("StartTime", new TimeSpan(0, 5, 0) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._trade_window_minutes = self.Param("TradeWindowMinutes", 5) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._intraday_candle_type = self.Param("IntradayCandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")

        self._daily_candle_type = None
        self._current_date = None
        self._trade_placed = False
        self._daily_ready = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._entry_price = 0.0

    def OnReseted(self):
        super(she_kanskigor_daily_strategy, self).OnReseted()
        self._daily_candle_type = None
        self._current_date = None
        self._trade_placed = False
        self._daily_ready = False
        self._previous_open = 0.0
        self._previous_close = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(she_kanskigor_daily_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        intraday = self.SubscribeCandles(Intradayself.candle_type)
        intraday.Bind(self._process_candle).Start()

        daily = self.SubscribeCandles(_dailyself.candle_type)
        daily.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return she_kanskigor_daily_strategy()
