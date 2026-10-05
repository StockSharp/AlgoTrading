import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class Conditions:
    """Sides that use take profit and stop loss."""
    NONE = 0
    Long = 1
    Short = 2
    Both = 3


class dual_rsi_differential_strategy(Strategy):
    """
    Dual RSI Differential strategy.
    The differential is RSI(LongRsiPeriod) minus RSI(ShortRsiPeriod). When it crosses below RsiDiffLevel the strategy goes long and
    when it crosses above RsiDiffLevel it goes short, reversing an opposite position. With UseHoldDays a position is closed after
    HoldDays days. Condition selects the sides that use the TakeProfitPerc and StopLossPerc exits.
    """

    def __init__(self):
        super(dual_rsi_differential_strategy, self).__init__()
        self._short_rsi_period = self.Param("ShortRsiPeriod", 21).SetGreaterThanZero().SetDisplay("Short RSI", "Short RSI period", "Indicators")
        self._long_rsi_period = self.Param("LongRsiPeriod", 42).SetGreaterThanZero().SetDisplay("Long RSI", "Long RSI period", "Indicators")
        self._rsi_diff_level = self.Param("RsiDiffLevel", 5.0).SetDisplay("RSI Diff Level", "Threshold of the RSI differential", "Indicators")
        self._use_hold_days = self.Param("UseHoldDays", True).SetDisplay("Use Hold Days", "Close positions after the holding period", "Exit")
        self._hold_days = self.Param("HoldDays", 5).SetGreaterThanZero().SetDisplay("Hold Days", "Holding period in days", "Exit")
        self._condition = self.Param("Condition", Conditions.NONE).SetDisplay("Condition", "Sides that use take profit and stop loss", "Risk")
        self._take_profit_perc = self.Param("TakeProfitPerc", 15.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_perc = self.Param("StopLossPerc", 10.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_diff = None
        self._entry_price = Decimal(0)
        self._entry_time = None

    def OnReseted(self):
        super(dual_rsi_differential_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dual_rsi_differential_strategy, self).OnStarted2(time)

        self._reset_state()

        short_rsi = RelativeStrengthIndex()
        short_rsi.Length = self._short_rsi_period.Value
        long_rsi = RelativeStrengthIndex()
        long_rsi.Length = self._long_rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_rsi, long_rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, short_rsi)
                self.DrawIndicator(oscillators, long_rsi)

    def _uses_risk_exits(self, is_long):
        condition = int(self._condition.Value)
        if condition == Conditions.Both:
            return True
        return condition == (Conditions.Long if is_long else Conditions.Short)

    def _process_candle(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not long_value.IsFormed:
            return

        diff = long_value.GetValue[Decimal](None) - short_value.GetValue[Decimal](None)
        prev = self._prev_diff
        self._prev_diff = diff

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position != 0:
            is_long = self.Position > 0
            exit_now = self._use_hold_days.Value and self._entry_time is not None and \
                candle.CloseTime - self._entry_time >= TimeSpan.FromDays(self._hold_days.Value)

            if not exit_now and self._uses_risk_exits(is_long) and self._entry_price > 0:
                take = Decimal(self._take_profit_perc.Value) / Decimal(100)
                stop = Decimal(self._stop_loss_perc.Value) / Decimal(100)
                one = Decimal(1)
                if is_long:
                    exit_now = (take > 0 and candle.HighPrice >= self._entry_price * (one + take)) or \
                        (stop > 0 and candle.LowPrice <= self._entry_price * (one - stop))
                else:
                    exit_now = (take > 0 and candle.LowPrice <= self._entry_price * (one - take)) or \
                        (stop > 0 and candle.HighPrice >= self._entry_price * (one + stop))

            if exit_now:
                if is_long:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
                return

        level = Decimal(self._rsi_diff_level.Value)
        long_signal = prev >= level and diff < level
        short_signal = prev <= level and diff > level

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._entry_time = candle.CloseTime
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._entry_time = candle.CloseTime

    def CreateClone(self):
        return dual_rsi_differential_strategy()
