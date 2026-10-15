import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

SIGNAL_BARS = 3


class five_ema_strategy(Strategy):
    """
    5 EMA strategy.
    A candle whose close and high are below the EMA marks a long setup, one whose close and low are above it marks a short setup.
    If price breaks the signal candle's high (long) or low (short) within the next three candles and outside the block window, the
    strategy enters in that direction with the stop at the signal candle's opposite extreme and the target at TargetRR times the risk.
    Open positions are closed at ExitHour:ExitMinute.
    """

    def __init__(self):
        super(five_ema_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 5).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period", "EMA")
        self._target_rr = self.Param("TargetRR", 3.0).SetGreaterThanZero().SetDisplay("Target R:R", "Reward to risk ratio of the target", "Risk")
        self._exit_hour = self.Param("ExitHour", 15).SetRange(0, 23).SetDisplay("Exit Hour", "Hour of the forced exit", "Time")
        self._exit_minute = self.Param("ExitMinute", 30).SetRange(0, 59).SetDisplay("Exit Minute", "Minute of the forced exit", "Time")
        self._block_start_hour = self.Param("BlockStartHour", 15).SetRange(0, 23).SetDisplay("Block Start Hour", "Hour the entry block starts", "Time")
        self._block_start_minute = self.Param("BlockStartMinute", 0).SetRange(0, 59).SetDisplay("Block Start Minute", "Minute the entry block starts", "Time")
        self._block_end_hour = self.Param("BlockEndHour", 15).SetRange(0, 23).SetDisplay("Block End Hour", "Hour the entry block ends", "Time")
        self._block_end_minute = self.Param("BlockEndMinute", 30).SetRange(0, 59).SetDisplay("Block End Minute", "Minute the entry block ends", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._signal_high = None
        self._signal_low = None
        self._signal_is_long = False
        self._bars_since_signal = 0
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(five_ema_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(five_ema_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed:
            return

        ema = ema_value.GetValue[Decimal](None)
        high = candle.HighPrice
        low = candle.LowPrice
        close = candle.ClosePrice

        if self._signal_high is not None:
            self._bars_since_signal += 1
            if self._bars_since_signal > SIGNAL_BARS:
                self._signal_high = None
                self._signal_low = None

        signal_high = self._signal_high
        signal_low = self._signal_low
        signal_is_long = self._signal_is_long

        # A new signal candle replaces the pending one and can only be broken by later candles.
        new_signal = True
        if close < ema and high < ema:
            self._set_signal(high, low, True)
        elif close > ema and low > ema:
            self._set_signal(high, low, False)
        else:
            new_signal = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tod = candle.OpenTime.TimeOfDay
        exit_time = TimeSpan(self._exit_hour.Value, self._exit_minute.Value, 0)
        frame = self.candle_type.Arg if isinstance(self.candle_type.Arg, TimeSpan) else TimeSpan.Zero

        if self.Position != 0 and tod <= exit_time and exit_time < tod + frame:
            self._close_position()
            return

        if self.Position > 0 and self._stop_price is not None and self._target_price is not None:
            if low <= self._stop_price or high >= self._target_price:
                self._close_position()
                return
        elif self.Position < 0 and self._stop_price is not None and self._target_price is not None:
            if high >= self._stop_price or low <= self._target_price:
                self._close_position()
                return

        if signal_high is None or signal_low is None:
            return

        block_start = TimeSpan(self._block_start_hour.Value, self._block_start_minute.Value, 0)
        block_end = TimeSpan(self._block_end_hour.Value, self._block_end_minute.Value, 0)
        if tod >= block_start and tod < block_end:
            return

        risk = signal_high - signal_low
        if risk <= 0:
            return

        target_rr = Decimal(self._target_rr.Value)

        if signal_is_long and high > signal_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = signal_low
            self._target_price = signal_high + risk * target_rr
            if not new_signal:
                self._signal_high = None
                self._signal_low = None
        elif not signal_is_long and low < signal_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = signal_high
            self._target_price = signal_low - risk * target_rr
            if not new_signal:
                self._signal_high = None
                self._signal_low = None

    def _set_signal(self, high, low, is_long):
        self._signal_high = high
        self._signal_low = low
        self._signal_is_long = is_long
        self._bars_since_signal = 0

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)
        self._stop_price = None
        self._target_price = None

    def CreateClone(self):
        return five_ema_strategy()
