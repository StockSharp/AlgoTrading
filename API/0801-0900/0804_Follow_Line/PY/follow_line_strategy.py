import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class _follow_line(object):
    def __init__(self):
        self.reset()

    def reset(self):
        self._line = None
        self.trend = 0
        self.prev_trend = 0

    def update(self, candle, upper, lower, atr, use_atr):
        prev_line = self._line
        line = prev_line
        close = float(candle.ClosePrice)

        if close > upper:
            candidate = float(candle.LowPrice) - atr if use_atr else float(candle.LowPrice)
            line = prev_line if prev_line is not None and candidate < prev_line else candidate
        elif close < lower:
            candidate = float(candle.HighPrice) + atr if use_atr else float(candle.HighPrice)
            line = prev_line if prev_line is not None and candidate > prev_line else candidate

        self.prev_trend = self.trend

        if line is not None and prev_line is not None:
            if line > prev_line:
                self.trend = 1
            elif line < prev_line:
                self.trend = -1

        self._line = line


class follow_line_strategy(Strategy):
    """
    Follow Line strategy.
    A close above the upper Bollinger Band moves the follow line up to the candle low (minus ATR with UseAtrFilter) but never down;
    a close below the lower band moves it down to the candle high (plus ATR) but never up; otherwise it stays. The trend turns up when
    the line rises and down when it falls. A turn up goes long and a turn down goes short, reversing an opposite position. With
    UseHtfConfirmation the same follow line on HtfCandleType must point the same way, and a position is closed when that trend turns
    against it. With UseTimeFilter entries happen only inside Session ("HHmm-HHmm", UTC).
    """

    def __init__(self):
        super(follow_line_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 5).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._bb_period = self.Param("BbPeriod", 21).SetGreaterThanZero().SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_deviation = self.Param("BbDeviation", 1.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
        self._use_atr_filter = self.Param("UseAtrFilter", True).SetDisplay("Use ATR Filter", "Offset the follow line by ATR", "Indicators")
        self._use_time_filter = self.Param("UseTimeFilter", False).SetDisplay("Use Time Filter", "Enter only inside the session", "Time")
        self._session = self.Param("Session", "0000-2400").SetDisplay("Session", "Trading session as HHmm-HHmm in UTC", "Time")
        self._use_htf_confirmation = self.Param("UseHtfConfirmation", False).SetDisplay("HTF Confirmation", "Require the higher timeframe follow line to agree", "Higher Timeframe")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._htf_candle_type = self.Param("HtfCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("HTF Candle Type", "Higher timeframe candles", "Higher Timeframe")
        self._line = _follow_line()
        self._htf_line = _follow_line()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        if self._use_htf_confirmation.Value:
            return [(self.Security, self.candle_type), (self.Security, self._htf_candle_type.Value)]
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(follow_line_strategy, self).OnReseted()
        self._line.reset()
        self._htf_line.reset()

    def OnStarted2(self, time):
        super(follow_line_strategy, self).OnStarted2(time)

        self._line.reset()
        self._htf_line.reset()

        bollinger = BollingerBands()
        bollinger.Length = self._bb_period.Value
        bollinger.Width = Decimal(self._bb_deviation.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, atr, self._process_candle).Start()

        if self._use_htf_confirmation.Value:
            htf_bollinger = BollingerBands()
            htf_bollinger.Length = self._bb_period.Value
            htf_bollinger.Width = Decimal(self._bb_deviation.Value)
            htf_atr = AverageTrueRange()
            htf_atr.Length = self._atr_period.Value
            self.SubscribeCandles(self._htf_candle_type.Value).BindEx(htf_bollinger, htf_atr, self._process_htf_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _bands(self, bb_value, atr_value):
        if not bb_value.IsFormed or not atr_value.IsFormed:
            return None
        if bb_value.UpBand is None or bb_value.LowBand is None:
            return None
        return float(bb_value.UpBand), float(bb_value.LowBand), float(atr_value.GetValue[Decimal](None))

    def _process_htf_candle(self, candle, bb_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        bands = self._bands(bb_value, atr_value)
        if bands is None:
            return

        self._htf_line.update(candle, bands[0], bands[1], bands[2], self._use_atr_filter.Value)

    def _process_candle(self, candle, bb_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        bands = self._bands(bb_value, atr_value)
        if bands is None:
            return

        self._line.update(candle, bands[0], bands[1], bands[2], self._use_atr_filter.Value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        use_htf = self._use_htf_confirmation.Value
        htf_trend = self._htf_line.trend

        if use_htf:
            if self.Position > 0 and htf_trend < 0:
                self.SellMarket(self.Position)
                return
            if self.Position < 0 and htf_trend > 0:
                self.BuyMarket(-self.Position)
                return

        turn_up = self._line.prev_trend == -1 and self._line.trend == 1
        turn_down = self._line.prev_trend == 1 and self._line.trend == -1

        if not turn_up and not turn_down:
            return

        can_enter = not self._use_time_filter.Value or self._in_session(candle.OpenTime)

        if turn_up:
            if can_enter and (not use_htf or htf_trend > 0) and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        else:
            if can_enter and (not use_htf or htf_trend < 0) and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def _in_session(self, time):
        parts = str(self._session.Value or "").split("-")
        try:
            start = int(parts[0])
            end = int(parts[1])
        except (ValueError, IndexError):
            return True
        if len(parts) != 2:
            return True

        start_minutes = start // 100 * 60 + start % 100
        end_minutes = end // 100 * 60 + end % 100
        minutes = time.Hour * 60 + time.Minute

        if start_minutes <= end_minutes:
            return start_minutes <= minutes < end_minutes
        return minutes >= start_minutes or minutes < end_minutes

    def CreateClone(self):
        return follow_line_strategy()
