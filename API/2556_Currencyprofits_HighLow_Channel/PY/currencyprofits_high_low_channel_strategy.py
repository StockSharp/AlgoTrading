import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, Highest, Lowest)
from StockSharp.Algo.Strategies import Strategy

MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_SMOOTHED = 2
MA_WEIGHTED = 3


class currencyprofits_high_low_channel_strategy(Strategy):
    def __init__(self):
        super(currencyprofits_high_low_channel_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 32)
        self._slow_length = self.Param("SlowLength", 86)
        self._channel_length = self.Param("ChannelLength", 12)
        self._stop_loss_points = self.Param("StopLossPoints", 170.0)
        self._risk_percent = self.Param("RiskPercent", 0.14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._fast_ma_type = self.Param("FastMaType", MA_SIMPLE)
        self._slow_ma_type = self.Param("SlowMaType", MA_SIMPLE)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4)

        self._previous_fast = None
        self._previous_slow = None
        self._previous_highest = None
        self._previous_lowest = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._processed_candles = 0
        self._cooldown_remaining = 0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def ChannelLength(self):
        return self._channel_length.Value

    @ChannelLength.setter
    def ChannelLength(self, value):
        self._channel_length.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMaType(self):
        return self._fast_ma_type.Value

    @FastMaType.setter
    def FastMaType(self, value):
        self._fast_ma_type.Value = value

    @property
    def SlowMaType(self):
        return self._slow_ma_type.Value

    @SlowMaType.setter
    def SlowMaType(self, value):
        self._slow_ma_type.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def _create_ma(self, ma_type, length):
        t = int(ma_type)
        if t == MA_EXPONENTIAL:
            ind = ExponentialMovingAverage()
            ind.Length = length
            return ind
        elif t == MA_SMOOTHED:
            ind = SmoothedMovingAverage()
            ind.Length = length
            return ind
        elif t == MA_WEIGHTED:
            ind = WeightedMovingAverage()
            ind.Length = length
            return ind
        else:
            ind = SimpleMovingAverage()
            ind.Length = length
            return ind

    def OnStarted2(self, time):
        super(currencyprofits_high_low_channel_strategy, self).OnStarted2(time)

        fast_ma = self._create_ma(self.FastMaType, self.FastLength)
        slow_ma = self._create_ma(self.SlowMaType, self.SlowLength)
        highest = Highest()
        highest.Length = self.ChannelLength
        lowest = Lowest()
        lowest.Length = self.ChannelLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, fast, slow, channel_high, channel_low):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        self._processed_candles += 1

        fast_val = float(fast)
        slow_val = float(slow)
        ch_high = float(channel_high)
        ch_low = float(channel_low)
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        required = max(max(int(self.FastLength), int(self.SlowLength)), int(self.ChannelLength)) + 1

        if self._processed_candles <= required:
            self._previous_fast = fast_val
            self._previous_slow = slow_val
            self._previous_highest = ch_high
            self._previous_lowest = ch_low
            return

        if self._previous_fast is None or self._previous_slow is None or self._previous_highest is None or self._previous_lowest is None:
            self._previous_fast = fast_val
            self._previous_slow = slow_val
            self._previous_highest = ch_high
            self._previous_lowest = ch_low
            return

        if self.Position > 0:
            exit_by_channel = close >= self._previous_highest
            exit_by_stop = self._stop_price > 0.0 and low <= self._stop_price
            if exit_by_channel or exit_by_stop:
                self.SellMarket()
                self._reset_trade_state()
                self._cooldown_remaining = int(self.SignalCooldownBars)

        elif self.Position < 0:
            exit_by_channel = close <= self._previous_lowest
            exit_by_stop = self._stop_price > 0.0 and high >= self._stop_price
            if exit_by_channel or exit_by_stop:
                self.BuyMarket()
                self._reset_trade_state()
                self._cooldown_remaining = int(self.SignalCooldownBars)

        elif self._cooldown_remaining == 0:
            stop_distance = self._get_stop_distance()

            if stop_distance > 0.0:
                bullish_trend = self._previous_fast > self._previous_slow and fast_val > slow_val
                bearish_trend = self._previous_fast < self._previous_slow and fast_val < slow_val
                bullish_reversal = low <= self._previous_lowest and close > open_price and close > fast_val
                bearish_reversal = high >= self._previous_highest and close < open_price and close < fast_val

                if bullish_trend and bullish_reversal:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = self._entry_price - stop_distance
                    self._cooldown_remaining = int(self.SignalCooldownBars)

                elif bearish_trend and bearish_reversal:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = self._entry_price + stop_distance
                    self._cooldown_remaining = int(self.SignalCooldownBars)

        self._previous_fast = fast_val
        self._previous_slow = slow_val
        self._previous_highest = ch_high
        self._previous_lowest = ch_low

    def _get_stop_distance(self):
        sl = float(self.StopLossPoints)
        if sl <= 0.0:
            return 0.0
        ps = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if ps > 0.0:
            return sl * ps
        return sl

    def _reset_trade_state(self):
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnReseted(self):
        super(currencyprofits_high_low_channel_strategy, self).OnReseted()
        self._previous_fast = None
        self._previous_slow = None
        self._previous_highest = None
        self._previous_lowest = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._processed_candles = 0
        self._cooldown_remaining = 0

    def CreateClone(self):
        return currencyprofits_high_low_channel_strategy()
