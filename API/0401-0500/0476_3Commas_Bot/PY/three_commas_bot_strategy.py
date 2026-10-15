import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class three_commas_bot_strategy(Strategy):
    """
    3Commas Bot Strategy (simplified).
    A long opens when the MaLength1 EMA crosses above the MaLength2 EMA and a short on the opposite cross, reversing an open
    position. The stop is RiskM ATRs from the entry and the reward threshold RnR times that risk. With UseTakeProfit the
    position closes at the threshold; with UseTrailingStop reaching it instead starts an ATR trailing stop that follows
    the best price at RiskM ATRs.
    """

    def __init__(self):
        super(three_commas_bot_strategy, self).__init__()
        self._ma_length1 = self.Param("MaLength1", 21).SetGreaterThanZero().SetDisplay("MA Length 1", "Fast EMA period", "Indicators")
        self._ma_length2 = self.Param("MaLength2", 50).SetGreaterThanZero().SetDisplay("MA Length 2", "Slow EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._rn_r = self.Param("RnR", 1.0).SetGreaterThanZero().SetDisplay("Reward/Risk", "Reward to risk ratio of the target", "Risk")
        self._risk_m = self.Param("RiskM", 1.0).SetGreaterThanZero().SetDisplay("Risk Multiplier", "ATR multiplier of the risk", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True).SetDisplay("Use Take Profit", "Close the position at the reward target", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", False).SetDisplay("Use Trailing Stop", "Trail the stop by ATR once the reward target is reached", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._clear_protection()

    def _clear_protection(self):
        self._stop_price = None
        self._target_price = None
        self._trailing = False

    def OnReseted(self):
        super(three_commas_bot_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(three_commas_bot_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._ma_length1.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._ma_length2.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not atr_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        risk = atr * float(self._risk_m.Value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0 and self._manage_long(high, low, risk):
            return

        if self.Position < 0 and self._manage_short(high, low, risk):
            return

        if prev_fast is None or prev_slow is None:
            return

        close = float(candle.ClosePrice)
        rn_r = float(self._rn_r.Value)

        if prev_fast <= prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._clear_protection()
            self._stop_price = close - risk
            self._target_price = close + risk * rn_r
        elif prev_fast >= prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._clear_protection()
            self._stop_price = close + risk
            self._target_price = close - risk * rn_r

    def _manage_long(self, high, low, risk):
        if self._target_price is not None and not self._trailing and high >= self._target_price:
            if self._use_trailing_stop.Value:
                self._trailing = True
            elif self._use_take_profit.Value:
                self.SellMarket(self.Position)
                self._clear_protection()
                return True

        if self._trailing:
            trailed = high - risk
            if self._stop_price is None or trailed > self._stop_price:
                self._stop_price = trailed

        if self._stop_price is not None and low <= self._stop_price:
            self.SellMarket(self.Position)
            self._clear_protection()
            return True

        return False

    def _manage_short(self, high, low, risk):
        if self._target_price is not None and not self._trailing and low <= self._target_price:
            if self._use_trailing_stop.Value:
                self._trailing = True
            elif self._use_take_profit.Value:
                self.BuyMarket(-self.Position)
                self._clear_protection()
                return True

        if self._trailing:
            trailed = low + risk
            if self._stop_price is None or trailed < self._stop_price:
                self._stop_price = trailed

        if self._stop_price is not None and high >= self._stop_price:
            self.BuyMarket(-self.Position)
            self._clear_protection()
            return True

        return False

    def CreateClone(self):
        return three_commas_bot_strategy()
