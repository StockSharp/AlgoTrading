import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ny_first_candle_break_and_retest_strategy(Strategy):
    """
    NY first candle break and retest strategy.
    The candle opening at NyStartHour:NyStartMinute (UTC) defines the session range. During the next SessionLength hours a close
    beyond its high or low by at least MinBreakSize ATR marks a breakout; a later candle that pulls back to within RetestThreshold
    ATR of the broken level and closes beyond it enters in the breakout direction, optionally only on the matching side of the EMA.
    The stop sits AtrMultiplier ATR from entry and the target RewardRiskRatio times that distance.
    """

    def __init__(self):
        super(ny_first_candle_break_and_retest_strategy, self).__init__()
        self._ny_start_hour = self.Param("NyStartHour", 9).SetRange(0, 23).SetDisplay("Start Hour", "Session start hour (UTC)", "Session")
        self._ny_start_minute = self.Param("NyStartMinute", 30).SetRange(0, 59).SetDisplay("Start Minute", "Session start minute", "Session")
        self._session_length = self.Param("SessionLength", 4).SetGreaterThanZero().SetDisplay("Session Length", "Session length in hours", "Session")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.2).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Stop distance in ATR", "Risk")
        self._reward_risk_ratio = self.Param("RewardRiskRatio", 1.5).SetGreaterThanZero().SetDisplay("Reward/Risk", "Target distance as a multiple of the stop distance", "Risk")
        self._min_break_size = self.Param("MinBreakSize", 0.15).SetNotNegative().SetDisplay("Min Break Size", "Minimum breakout beyond the first candle in ATR", "Entry")
        self._retest_threshold = self.Param("RetestThreshold", 0.25).SetNotNegative().SetDisplay("Retest Threshold", "Maximum retest distance from the broken level in ATR", "Entry")
        self._use_ema_filter = self.Param("UseEmaFilter", True).SetDisplay("Use EMA Filter", "Require the close on the trade side of the EMA", "Entry")
        self._ema_length = self.Param("EmaLength", 13).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._range_high = None
        self._range_low = None
        self._broke_up = False
        self._broke_down = False
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(ny_first_candle_break_and_retest_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ny_first_candle_break_and_retest_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr, ema):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        day = open_time.Date

        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._range_high = None
            self._range_low = None
            self._broke_up = False
            self._broke_down = False

        session_start = day.Add(TimeSpan(self._ny_start_hour.Value, self._ny_start_minute.Value, 0))
        session_end = session_start.Add(TimeSpan.FromHours(self._session_length.Value))

        if open_time == session_start:
            self._range_high = candle.HighPrice
            self._range_low = candle.LowPrice
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if open_time <= session_start or open_time >= session_end or self._range_high is None or self._range_low is None:
            return

        high = self._range_high
        low = self._range_low
        stop_distance = atr * Decimal(self._atr_multiplier.Value)
        reward_risk = Decimal(self._reward_risk_ratio.Value)
        retest = Decimal(self._retest_threshold.Value) * atr
        use_ema = self._use_ema_filter.Value

        if self._broke_up and self.Position <= 0 and candle.LowPrice <= high + retest and close > high and (not use_ema or close > ema):
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance
            self._take_price = close + stop_distance * reward_risk
            self._broke_up = False
            return

        if self._broke_down and self.Position >= 0 and candle.HighPrice >= low - retest and close < low and (not use_ema or close < ema):
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance
            self._take_price = close - stop_distance * reward_risk
            self._broke_down = False
            return

        # A breakout candle only arms the setup; the retest has to come on a later candle.
        min_break = Decimal(self._min_break_size.Value) * atr
        if close > high + min_break:
            self._broke_up = True
        elif close < low - min_break:
            self._broke_down = True

    def CreateClone(self):
        return ny_first_candle_break_and_retest_strategy()
