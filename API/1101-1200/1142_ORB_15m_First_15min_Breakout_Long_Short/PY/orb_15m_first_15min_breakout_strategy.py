import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, TimeZoneInfo, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class orb_15m_first_15min_breakout_strategy(Strategy):
    """
    ORB 15m first 15 minute breakout strategy.
    At the close of the first 15 minute bar after the session open (Stockholm time) a bullish bar opens a long and a bearish bar a short.
    The stop sits at the opposite extreme of that bar, the size risks RiskPct of equity on the stop distance, the optional target is
    RMultiple times the risk, and any open position is closed at the session end.
    """

    def __init__(self):
        super(orb_15m_first_15min_breakout_strategy, self).__init__()
        self._risk_pct = self.Param("RiskPct", 1.0).SetGreaterThanZero().SetDisplay("Risk %", "Percent of equity risked per trade", "Risk")
        self._tp_ten_r = self.Param("TpTenR", True).SetDisplay("Use Take Profit", "Take profit at RMultiple times risk", "Risk")
        self._r_multiple = self.Param("RMultiple", 10.0).SetGreaterThanZero().SetDisplay("R Multiple", "Take profit as a multiple of risk", "Risk")
        self._session_open_hour = self.Param("SessionOpenHour", 15).SetRange(0, 23).SetDisplay("Session Open Hour", "Session open hour (Stockholm time)", "Session")
        self._session_open_minute = self.Param("SessionOpenMinute", 30).SetRange(0, 59).SetDisplay("Session Open Minute", "Session open minute (Stockholm time)", "Session")
        self._session_end_hour = self.Param("SessionEndHour", 22).SetRange(0, 23).SetDisplay("Session End Hour", "Session end hour (Stockholm time)", "Session")
        self._session_end_minute = self.Param("SessionEndMinute", 0).SetRange(0, 59).SetDisplay("Session End Minute", "Session end minute (Stockholm time)", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._time_zone = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._traded_today = False
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(orb_15m_first_15min_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(orb_15m_first_15min_breakout_strategy, self).OnStarted2(time)

        self._reset_state()
        self._time_zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm")

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime.ToUniversalTime(), self._time_zone)
        tod = local.TimeOfDay
        session_open = TimeSpan(self._session_open_hour.Value, self._session_open_minute.Value, 0)
        session_end = TimeSpan(self._session_end_hour.Value, self._session_end_minute.Value, 0)

        if self._current_day is None or local.Date != self._current_day:
            self._current_day = local.Date
            self._traded_today = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            close_time = local.Add(candle.CloseTime.Subtract(candle.OpenTime))
            session_over = close_time.TimeOfDay.TotalMinutes >= session_end.TotalMinutes

            if self.Position > 0:
                if (self._stop_price is not None and candle.LowPrice <= self._stop_price) or \
                        (self._take_price is not None and candle.HighPrice >= self._take_price) or session_over:
                    self.SellMarket(self.Position)
                    self._stop_price = None
                    self._take_price = None
            else:
                if (self._stop_price is not None and candle.HighPrice >= self._stop_price) or \
                        (self._take_price is not None and candle.LowPrice <= self._take_price) or session_over:
                    self.BuyMarket(-self.Position)
                    self._stop_price = None
                    self._take_price = None
            return

        # Only the first bar of the session is the reference bar.
        if self._traded_today or tod.TotalMinutes != session_open.TotalMinutes:
            return

        self._traded_today = True

        is_long = candle.ClosePrice > candle.OpenPrice
        is_short = candle.ClosePrice < candle.OpenPrice
        if not is_long and not is_short:
            return

        entry = candle.ClosePrice
        stop = candle.LowPrice if is_long else candle.HighPrice
        risk = abs(entry - stop)
        if risk <= 0:
            return

        volume = self.Volume
        equity = self.Portfolio.CurrentValue if self.Portfolio is not None else None
        if equity is not None and equity > 0:
            risk_volume = equity * Decimal(self._risk_pct.Value) / Decimal(100) / risk
            if risk_volume > 0:
                volume = risk_volume

        r_multiple = Decimal(self._r_multiple.Value)
        self._stop_price = stop
        if self._tp_ten_r.Value:
            self._take_price = entry + risk * r_multiple if is_long else entry - risk * r_multiple
        else:
            self._take_price = None

        if is_long:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

    def CreateClone(self):
        return orb_15m_first_15min_breakout_strategy()
