import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
                                        WeightedMovingAverage)
from StockSharp.Algo.Strategies import Strategy

# Values matching the C# TradeDirections enum.
DIRECTION_LONG_ONLY = 0
DIRECTION_SHORT_ONLY = 1
DIRECTION_BOTH = 2

# Values matching the C# TakeProfitTypes enum.
TP_FIXED_RISK_REWARD = 0
TP_MA_CROSS = 1
TP_BOTH = 2

# Values matching the C# MaTypes enum.
MA_SMA = 0
MA_EMA = 1
MA_SMMA = 2
MA_WMA = 3

RANGE_START = TimeSpan(9, 30, 0)
RANGE_END = TimeSpan(9, 45, 0)


class ny_opening_range_breakout_ma_stop_strategy(Strategy):
    """
    NY opening range breakout with MA stop strategy.
    The 9:30-9:45 (UTC candle open times) range is recorded each day. When the previous candle is the first to close beyond the range
    high (low) before CutoffHour:CutoffMinute, the current candle enters long (short) if its close is on the same side of the moving
    average and the side is allowed by TradeDirection. The stop sits at the opposite side of the range; the exit target follows
    TakeProfitType: TpRatio times the risk, a close back across the moving average, or whichever comes first.
    """

    def __init__(self):
        super(ny_opening_range_breakout_ma_stop_strategy, self).__init__()
        self._cutoff_hour = self.Param("CutoffHour", 12).SetRange(0, 23).SetDisplay("Cutoff Hour", "Hour after which no new breakouts are taken", "Session")
        self._cutoff_minute = self.Param("CutoffMinute", 0).SetRange(0, 59).SetDisplay("Cutoff Minute", "Minute of the cutoff time", "Session")
        self._trade_direction = self.Param("TradeDirection", DIRECTION_LONG_ONLY).SetDisplay("Trade Direction", "Allowed trade direction (0 LongOnly, 1 ShortOnly, 2 Both)", "Trading")
        self._take_profit_type = self.Param("TakeProfitType", TP_FIXED_RISK_REWARD).SetDisplay("Take Profit Type", "0 FixedRiskReward, 1 MaCross, 2 Both", "Risk")
        self._tp_ratio = self.Param("TpRatio", 2.5).SetGreaterThanZero().SetDisplay("TP Ratio", "Reward-to-risk ratio of the fixed target", "Risk")
        self._ma_type = self.Param("MaType", MA_SMA).SetDisplay("MA Type", "Moving average type (0 SMA, 1 EMA, 2 SMMA, 3 WMA)", "Moving Average")
        self._ma_length = self.Param("MaLength", 100).SetGreaterThanZero().SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._range_high = None
        self._range_low = None
        self._prev_close = None
        self._prev_prev_close = None
        self._prev_before_cutoff = False
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(ny_opening_range_breakout_ma_stop_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ny_opening_range_breakout_ma_stop_strategy, self).OnStarted2(time)

        self._reset_state()

        ma_type = self._ma_type.Value
        if ma_type == MA_EMA:
            ma = ExponentialMovingAverage()
        elif ma_type == MA_SMMA:
            ma = SmoothedMovingAverage()
        elif ma_type == MA_WMA:
            ma = WeightedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        tod = candle.OpenTime.TimeOfDay

        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._range_high = None
            self._range_low = None
            self._prev_close = None
            self._prev_prev_close = None
            self._prev_before_cutoff = False

        close = candle.ClosePrice

        if tod >= RANGE_START and tod < RANGE_END:
            self._range_high = candle.HighPrice if self._range_high is None else max(self._range_high, candle.HighPrice)
            self._range_low = candle.LowPrice if self._range_low is None else min(self._range_low, candle.LowPrice)
            return

        prev_close = self._prev_close
        prev_prev_close = self._prev_prev_close
        prev_before_cutoff = self._prev_before_cutoff
        cutoff = TimeSpan(self._cutoff_hour.Value, self._cutoff_minute.Value, 0)

        if tod >= RANGE_END:
            self._prev_prev_close = self._prev_close
            self._prev_close = close
            self._prev_before_cutoff = tod < cutoff

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tp_type = self._take_profit_type.Value
        use_fixed = tp_type != TP_MA_CROSS
        use_ma = tp_type != TP_FIXED_RISK_REWARD

        if self.Position > 0:
            if ((self._stop_price is not None and candle.LowPrice <= self._stop_price)
                    or (use_fixed and self._take_price is not None and candle.HighPrice >= self._take_price)
                    or (use_ma and close < ma_value)):
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0:
            if ((self._stop_price is not None and candle.HighPrice >= self._stop_price)
                    or (use_fixed and self._take_price is not None and candle.LowPrice <= self._take_price)
                    or (use_ma and close > ma_value)):
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if self._range_high is None or self._range_low is None or prev_close is None or not prev_before_cutoff:
            return

        high = self._range_high
        low = self._range_low

        # The previous candle must be the first close beyond the range.
        broke_up = prev_close > high and (prev_prev_close is None or prev_prev_close <= high)
        broke_down = prev_close < low and (prev_prev_close is None or prev_prev_close >= low)

        direction = self._trade_direction.Value
        allow_long = direction != DIRECTION_SHORT_ONLY
        allow_short = direction != DIRECTION_LONG_ONLY
        ratio = Decimal(self._tp_ratio.Value)

        if broke_up and allow_long and close > ma_value and close > low and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = low
            self._take_price = close + (close - low) * ratio
        elif broke_down and allow_short and close < ma_value and close < high and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = high
            self._take_price = close - (high - close) * ratio

    def CreateClone(self):
        return ny_opening_range_breakout_ma_stop_strategy()
