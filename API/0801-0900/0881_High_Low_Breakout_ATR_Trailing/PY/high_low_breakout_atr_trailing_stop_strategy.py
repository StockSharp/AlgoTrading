import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

# Direction values, matching the C# TradeDirections enum.
DIRECTION_LONG = 0
DIRECTION_SHORT = 1
DIRECTION_BOTH = 2

class high_low_breakout_atr_trailing_stop_strategy(Strategy):
    """
    High-Low Breakout ATR Trailing Stop strategy.
    The first candle opening at or after SessionStartHour:SessionStartMinute (UTC) sets the day's opening range. A close crossing
    above its high buys and a close crossing below its low sells, within the allowed Direction. The stop trails AtrMultiplier ATRs
    behind the close and a target sits the same distance from the entry. The position size risks RiskPerTrade percent of
    AccountSize on the stop distance. Everything is closed at ExitHour:ExitMinute and no new trades open after it.
    """

    def __init__(self):
        super(high_low_breakout_atr_trailing_stop_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Trailing stop and target distance in ATRs", "Risk")
        self._risk_per_trade = self.Param("RiskPerTrade", 2.0).SetGreaterThanZero().SetDisplay("Risk Per Trade %", "Percent of the account risked per trade", "Risk")
        self._account_size = self.Param("AccountSize", 10000.0).SetGreaterThanZero().SetDisplay("Account Size", "Account size used for position sizing", "Risk")
        self._session_start_hour = self.Param("SessionStartHour", 9).SetRange(0, 23).SetDisplay("Session Start Hour", "Session start hour (UTC)", "Session")
        self._session_start_minute = self.Param("SessionStartMinute", 15).SetRange(0, 59).SetDisplay("Session Start Minute", "Session start minute", "Session")
        self._exit_hour = self.Param("ExitHour", 15).SetRange(0, 23).SetDisplay("Exit Hour", "Hour all positions are closed (UTC)", "Session")
        self._exit_minute = self.Param("ExitMinute", 15).SetRange(0, 59).SetDisplay("Exit Minute", "Minute all positions are closed", "Session")
        self._direction = self.Param("Direction", DIRECTION_BOTH).SetDisplay("Direction", "Allowed trade directions (0 Long, 1 Short, 2 Both)", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._range_date = None
        self._range_high = None
        self._range_low = None
        self._prev_close = None
        self._stop_price = 0.0
        self._target_price = 0.0

    def OnReseted(self):
        super(high_low_breakout_atr_trailing_stop_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(high_low_breakout_atr_trailing_stop_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_entry_volume(self, stop_distance):
        risk = float(self._account_size.Value) * float(self._risk_per_trade.Value) / 100.0
        volume = risk / stop_distance if stop_distance > 0 else 0.0

        sec = self.Security
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0.0
        if step > 0:
            volume = math.floor(volume / step) * step

        min_volume = float(sec.MinVolume) if sec is not None and sec.MinVolume is not None else 0.0
        if volume <= 0 or volume < min_volume:
            return self.Volume

        max_volume = float(sec.MaxVolume) if sec is not None and sec.MaxVolume is not None else 0.0
        if max_volume > 0 and volume > max_volume:
            volume = max_volume

        return Decimal(round(volume, 8))

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime
        minutes = open_time.Hour * 60 + open_time.Minute
        session_start = self._session_start_hour.Value * 60 + self._session_start_minute.Value
        exit_time = self._exit_hour.Value * 60 + self._exit_minute.Value
        close = float(candle.ClosePrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        prev_close = self._prev_close
        self._prev_close = close

        # The first candle of the session sets the opening range.
        if self._range_date != open_time.Date and minutes >= session_start:
            self._range_date = open_time.Date
            self._range_high = high_price
            self._range_low = low_price
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if minutes >= exit_time or minutes < session_start:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        distance = float(self._atr_multiplier.Value) * float(atr_value)

        if self.Position > 0:
            if low_price <= self._stop_price or high_price >= self._target_price:
                self.SellMarket(self.Position)
                return
            self._stop_price = max(self._stop_price, close - distance)
            return

        if self.Position < 0:
            if high_price >= self._stop_price or low_price <= self._target_price:
                self.BuyMarket(-self.Position)
                return
            self._stop_price = min(self._stop_price, close + distance)
            return

        if self._range_date != open_time.Date or self._range_high is None or self._range_low is None or prev_close is None:
            return

        direction = int(self._direction.Value)

        if direction != DIRECTION_SHORT and prev_close <= self._range_high and close > self._range_high:
            self.BuyMarket(self._get_entry_volume(distance))
            self._stop_price = close - distance
            self._target_price = close + distance
        elif direction != DIRECTION_LONG and prev_close >= self._range_low and close < self._range_low:
            self.SellMarket(self._get_entry_volume(distance))
            self._stop_price = close + distance
            self._target_price = close - distance

    def CreateClone(self):
        return high_low_breakout_atr_trailing_stop_strategy()
