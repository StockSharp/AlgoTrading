import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, DecimalIndicatorValue
from StockSharp.Algo.Indicators import (
    CommodityChannelIndex, RelativeStrengthIndex, SimpleMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy


# Signal mode constants
MODE_TREND = 0
MODE_FLAT = 1


class gg_rsi_cci_strategy(Strategy):

    def __init__(self):
        super(gg_rsi_cci_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame for indicator calculation.", "General")
        self._length = self.Param("Length", 8) \
            .SetDisplay("Length", "RSI and CCI period.", "Indicators")
        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Fast Period", "Fast smoothing period.", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 8) \
            .SetDisplay("Slow Period", "Slow smoothing period.", "Indicators")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Buy", "Permit opening long positions.", "Permissions")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Sell", "Permit opening short positions.", "Permissions")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Close Short", "Permit closing short positions.", "Permissions")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Close Long", "Permit closing long positions.", "Permissions")
        self._mode = self.Param("Mode", MODE_TREND) \
            .SetDisplay("Mode", "Closing style.", "Trading")

        self._rsi_fast = None
        self._rsi_slow = None
        self._cci_fast = None
        self._cci_slow = None
        self._prev_signal = -1

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def AllowBuyOpen(self):
        return self._allow_buy_open.Value

    @AllowBuyOpen.setter
    def AllowBuyOpen(self, value):
        self._allow_buy_open.Value = value

    @property
    def AllowSellOpen(self):
        return self._allow_sell_open.Value

    @AllowSellOpen.setter
    def AllowSellOpen(self, value):
        self._allow_sell_open.Value = value

    @property
    def AllowBuyClose(self):
        return self._allow_buy_close.Value

    @AllowBuyClose.setter
    def AllowBuyClose(self, value):
        self._allow_buy_close.Value = value

    @property
    def AllowSellClose(self):
        return self._allow_sell_close.Value

    @AllowSellClose.setter
    def AllowSellClose(self, value):
        self._allow_sell_close.Value = value

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    def OnStarted(self, time):
        super(gg_rsi_cci_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Length
        cci = CommodityChannelIndex()
        cci.Length = self.Length

        self._rsi_fast = SimpleMovingAverage()
        self._rsi_fast.Length = self.FastPeriod
        self._rsi_slow = SimpleMovingAverage()
        self._rsi_slow.Length = self.SlowPeriod
        self._cci_fast = SimpleMovingAverage()
        self._cci_fast.Length = self.FastPeriod
        self._cci_slow = SimpleMovingAverage()
        self._cci_slow.Length = self.SlowPeriod

        self._prev_signal = -1

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, cci, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_val = float(rsi_value)
        cci_val = float(cci_value)

        rsi_fast = float(self._rsi_fast.Process(
            DecimalIndicatorValue(self._rsi_fast, rsi_val, candle.OpenTime, True)))
        rsi_slow = float(self._rsi_slow.Process(
            DecimalIndicatorValue(self._rsi_slow, rsi_val, candle.OpenTime, True)))
        cci_fast = float(self._cci_fast.Process(
            DecimalIndicatorValue(self._cci_fast, cci_val, candle.OpenTime, True)))
        cci_slow = float(self._cci_slow.Process(
            DecimalIndicatorValue(self._cci_slow, cci_val, candle.OpenTime, True)))

        if rsi_fast > rsi_slow and cci_fast > cci_slow and cci_val > 0.0:
            signal = 2
        elif rsi_fast < rsi_slow and cci_fast < cci_slow and cci_val < 0.0:
            signal = 0
        else:
            signal = 1

        pos = self.Position

        if signal == 2:
            if self.AllowSellClose and pos < 0:
                self.BuyMarket(abs(pos))

            if self.AllowBuyOpen and pos <= 0 and self._prev_signal != 2:
                self.BuyMarket(self.Volume + abs(self.Position))

        elif signal == 0:
            if self.AllowBuyClose and pos > 0:
                self.SellMarket(pos)

            if self.AllowSellOpen and pos >= 0 and self._prev_signal != 0:
                self.SellMarket(self.Volume + abs(self.Position))

        elif self.Mode == MODE_FLAT:
            if self.AllowBuyClose and pos > 0:
                self.SellMarket(pos)
            if self.AllowSellClose and pos < 0:
                self.BuyMarket(abs(pos))

        self._prev_signal = signal

    def OnReseted(self):
        super(gg_rsi_cci_strategy, self).OnReseted()
        if self._rsi_fast is not None:
            self._rsi_fast.Reset()
        if self._rsi_slow is not None:
            self._rsi_slow.Reset()
        if self._cci_fast is not None:
            self._cci_fast.Reset()
        if self._cci_slow is not None:
            self._cci_slow.Reset()
        self._prev_signal = -1

    def CreateClone(self):
        return gg_rsi_cci_strategy()
