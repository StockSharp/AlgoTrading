import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class turnaround_tuesday_strategy(Strategy):
    """
    Turnaround Tuesday trading strategy.
    Buys if previous session declined and price above MA.
    Sells if previous session rallied and price below MA.
    Uses session detection via day-of-year transitions.
    """

    def __init__(self):
        super(turnaround_tuesday_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_ma = 0.0
        self._session_open = 0.0
        self._session_close = 0.0
        self._prev_session_day = -1
        self._prev_session_decline = False
        self._prev_session_rally = False
        self._current_session_day = -1
        self._entered_this_session = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(turnaround_tuesday_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._session_open = 0.0
        self._session_close = 0.0
        self._prev_session_day = -1
        self._prev_session_decline = False
        self._prev_session_rally = False
        self._current_session_day = -1
        self._entered_this_session = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(turnaround_tuesday_strategy, self).OnStarted2(time)

        self._prev_ma = 0.0
        self._session_open = 0.0
        self._session_close = 0.0
        self._prev_session_day = -1
        self._prev_session_decline = False
        self._prev_session_rally = False
        self._current_session_day = -1
        self._entered_this_session = False
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        day_of_year = candle.OpenTime.DayOfYear
        cd = self._cooldown_bars.Value

        # Detect new session (new calendar day)
        if day_of_year != self._current_session_day:
            # Save previous session result
            if self._current_session_day >= 0 and self._session_open > 0:
                self._prev_session_decline = self._session_close < self._session_open
                self._prev_session_rally = self._session_close > self._session_open
                self._prev_session_day = self._current_session_day

            self._current_session_day = day_of_year
            self._session_open = float(candle.OpenPrice)
            self._entered_this_session = False

        self._session_close = close

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma = ma
            return

        # Entry: buy if previous session declined and no position
        if self.Position == 0 and not self._entered_this_session and self._prev_session_decline and close > ma:
            self.BuyMarket()
            self._cooldown = cd
            self._entered_this_session = True
            self._prev_session_decline = False
        # Entry: sell if previous session rallied and no position
        elif self.Position == 0 and not self._entered_this_session and self._prev_session_rally and close < ma:
            self.SellMarket()
            self._cooldown = cd
            self._entered_this_session = True
            self._prev_session_rally = False

        # Exit long if price crosses below MA
        if self.Position > 0 and self._prev_ma > 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd

        # Exit short if price crosses above MA
        if self.Position < 0 and self._prev_ma > 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_ma = ma

    def CreateClone(self):
        return turnaround_tuesday_strategy()
