import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class adaptive_trader_pro_strategy(Strategy):
    def __init__(self):
        super(adaptive_trader_pro_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._trailing_stop_multiplier = self.Param("TrailingStopMultiplier", 3.0)
        self._break_even_multiplier = self.Param("BreakEvenMultiplier", 1.5)
        self._trend_period = self.Param("TrendPeriod", 20)

        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._trailing_stop_level = 0.0
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def TrailingStopMultiplier(self):
        return self._trailing_stop_multiplier.Value

    @TrailingStopMultiplier.setter
    def TrailingStopMultiplier(self, value):
        self._trailing_stop_multiplier.Value = value

    @property
    def BreakEvenMultiplier(self):
        return self._break_even_multiplier.Value

    @BreakEvenMultiplier.setter
    def BreakEvenMultiplier(self, value):
        self._break_even_multiplier.Value = value

    @property
    def TrendPeriod(self):
        return self._trend_period.Value

    @TrendPeriod.setter
    def TrendPeriod(self, value):
        self._trend_period.Value = value

    def OnReseted(self):
        super(adaptive_trader_pro_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._trailing_stop_level = 0.0
        self._direction = 0

    def OnStarted(self, time):
        super(adaptive_trader_pro_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._trailing_stop_level = 0.0
        self._direction = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        trend_ma = SimpleMovingAverage()
        trend_ma.Length = self.TrendPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, atr, trend_ma, self._process_candle).Start()

    def _reset_trade_state(self):
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._break_even_applied = False
        self._trailing_stop_level = 0.0
        self._direction = 0

    def _process_candle(self, candle, rsi_value, atr_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        atr_val = float(atr_value)
        trend_val = float(trend_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail_mult = float(self.TrailingStopMultiplier)
        be_mult = float(self.BreakEvenMultiplier)

        # Trailing stop management
        if self._direction > 0 and self.Position > 0:
            atr_for_targets = self._entry_atr if self._entry_atr > 0 else atr_val
            trailing_distance = atr_val * trail_mult
            candidate_stop = close - trailing_distance

            if self._trailing_stop_level <= 0 or candidate_stop > self._trailing_stop_level:
                self._trailing_stop_level = candidate_stop

            if not self._break_even_applied and atr_for_targets > 0:
                be_trigger = self._entry_price + atr_for_targets * be_mult
                if high >= be_trigger:
                    self._trailing_stop_level = max(self._trailing_stop_level, self._entry_price)
                    self._break_even_applied = True

            if self._trailing_stop_level > 0 and low <= self._trailing_stop_level:
                self.SellMarket()
                self._reset_trade_state()
                return

        elif self._direction < 0 and self.Position < 0:
            atr_for_targets = self._entry_atr if self._entry_atr > 0 else atr_val
            trailing_distance = atr_val * trail_mult
            candidate_stop = close + trailing_distance

            if self._trailing_stop_level <= 0 or candidate_stop < self._trailing_stop_level:
                self._trailing_stop_level = candidate_stop

            if not self._break_even_applied and atr_for_targets > 0:
                be_trigger = self._entry_price - atr_for_targets * be_mult
                if low <= be_trigger:
                    self._trailing_stop_level = min(self._trailing_stop_level, self._entry_price)
                    self._break_even_applied = True

            if self._trailing_stop_level > 0 and high >= self._trailing_stop_level:
                self.BuyMarket()
                self._reset_trade_state()
                return
        else:
            if self.Position == 0:
                self._reset_trade_state()

        if self.Position != 0:
            return

        if atr_val <= 0:
            return

        # Entry logic
        if rsi_val < 45.0 and close > trend_val:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._entry_atr = atr_val
            self._direction = 1
            self._trailing_stop_level = close - atr_val * trail_mult
            self._break_even_applied = False
        elif rsi_val > 55.0 and close < trend_val:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._entry_atr = atr_val
            self._direction = -1
            self._trailing_stop_level = close + atr_val * trail_mult
            self._break_even_applied = False

    def CreateClone(self):
        return adaptive_trader_pro_strategy()
