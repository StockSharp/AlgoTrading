import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class tds_global_strategy(Strategy):
    def __init__(self):
        super(tds_global_strategy, self).__init__()

        self._macd_fast_length = self.Param("MacdFastLength", 12)
        self._macd_slow_length = self.Param("MacdSlowLength", 23)
        self._macd_signal_length = self.Param("MacdSignalLength", 9)
        self._williams_length = self.Param("WilliamsLength", 24)
        self._williams_buy_level = self.Param("WilliamsBuyLevel", 30.0)
        self._williams_sell_level = self.Param("WilliamsSellLevel", 70.0)
        self._entry_buffer_steps = self.Param("EntryBufferSteps", 16)
        self._take_profit_steps = self.Param("TakeProfitSteps", 999.0)
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._price_step = 1.0
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._williams_prev1 = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @MacdFastLength.setter
    def MacdFastLength(self, value):
        self._macd_fast_length.Value = value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @MacdSlowLength.setter
    def MacdSlowLength(self, value):
        self._macd_slow_length.Value = value

    @property
    def MacdSignalLength(self):
        return self._macd_signal_length.Value

    @MacdSignalLength.setter
    def MacdSignalLength(self, value):
        self._macd_signal_length.Value = value

    @property
    def WilliamsLength(self):
        return self._williams_length.Value

    @WilliamsLength.setter
    def WilliamsLength(self, value):
        self._williams_length.Value = value

    @property
    def WilliamsBuyLevel(self):
        return self._williams_buy_level.Value

    @WilliamsBuyLevel.setter
    def WilliamsBuyLevel(self, value):
        self._williams_buy_level.Value = value

    @property
    def WilliamsSellLevel(self):
        return self._williams_sell_level.Value

    @WilliamsSellLevel.setter
    def WilliamsSellLevel(self, value):
        self._williams_sell_level.Value = value

    @property
    def EntryBufferSteps(self):
        return self._entry_buffer_steps.Value

    @EntryBufferSteps.setter
    def EntryBufferSteps(self, value):
        self._entry_buffer_steps.Value = value

    @property
    def TakeProfitSteps(self):
        return self._take_profit_steps.Value

    @TakeProfitSteps.setter
    def TakeProfitSteps(self, value):
        self._take_profit_steps.Value = value

    @property
    def TrailingStopSteps(self):
        return self._trailing_stop_steps.Value

    @TrailingStopSteps.setter
    def TrailingStopSteps(self, value):
        self._trailing_stop_steps.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(tds_global_strategy, self).OnStarted(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._price_step <= 0.0:
            self._price_step = 1.0

        self._macd_prev1 = None
        self._macd_prev2 = None
        self._williams_prev1 = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.MacdFastLength
        macd.LongMa.Length = self.MacdSlowLength

        williams = RelativeStrengthIndex()
        williams.Length = self.WilliamsLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, williams, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, macd_line, williams):
        if candle.State != CandleStates.Finished:
            return

        macd_val = float(macd_line)
        williams_val = float(williams)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._manage_open_position(candle)

        if abs(self.Position) > 0:
            self._update_history(macd_val, williams_val, candle)
            return

        if self._macd_prev1 is None or self._macd_prev2 is None or \
           self._williams_prev1 is None or self._prev_high is None or \
           self._prev_low is None or self._prev_close is None:
            self._update_history(macd_val, williams_val, candle)
            return

        direction = 1 if self._macd_prev1 > self._macd_prev2 else (-1 if self._macd_prev1 < self._macd_prev2 else 0)
        oversold = self._williams_prev1 <= float(self.WilliamsBuyLevel)
        overbought = self._williams_prev1 >= float(self.WilliamsSellLevel)

        if direction > 0 and oversold:
            self.BuyMarket()
            entry = close
            tp_steps = float(self.TakeProfitSteps)
            self._entry_price = entry
            self._stop_price = self._prev_low - self._price_step
            self._take_price = entry + tp_steps * self._price_step if tp_steps > 0.0 else None
        elif direction < 0 and overbought:
            self.SellMarket()
            entry = close
            tp_steps = float(self.TakeProfitSteps)
            self._entry_price = entry
            self._stop_price = self._prev_high + self._price_step
            self._take_price = entry - tp_steps * self._price_step if tp_steps > 0.0 else None

        self._update_history(macd_val, williams_val, candle)

    def _manage_open_position(self, candle):
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._entry_price is None:
                self._entry_price = close

            trail_steps = float(self.TrailingStopSteps)
            if trail_steps > 0.0:
                trailing = close - trail_steps * self._price_step
                if self._stop_price is None or trailing > self._stop_price:
                    self._stop_price = trailing

            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_position()
                return

            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_position()

        elif self.Position < 0:
            if self._entry_price is None:
                self._entry_price = close

            trail_steps = float(self.TrailingStopSteps)
            if trail_steps > 0.0:
                trailing = close + trail_steps * self._price_step
                if self._stop_price is None or trailing < self._stop_price:
                    self._stop_price = trailing

            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_position()
                return

            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_position()
        else:
            self._reset_position()

    def _update_history(self, macd_val, williams_val, candle):
        if self._macd_prev1 is not None:
            self._macd_prev2 = self._macd_prev1

        self._macd_prev1 = macd_val
        self._williams_prev1 = williams_val
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_close = float(candle.ClosePrice)

    def _reset_position(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(tds_global_strategy, self).OnReseted()
        self._price_step = 1.0
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._williams_prev1 = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._reset_position()

    def CreateClone(self):
        return tds_global_strategy()
