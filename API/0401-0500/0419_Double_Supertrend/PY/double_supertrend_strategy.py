import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class double_supertrend_strategy(Strategy):
    """
    Double Supertrend strategy.
    A position opens in the allowed Direction ("Long", "Short" or "Both") when the close moves above (below) both
    Supertrend lines. A close back through the first line exits. With TPType "Supertrend" the second line also works as
    a trailing exit; with TPType "Percent" a TPPercent take-profit is used instead. SLPercent is a percent stop-loss.
    """

    def __init__(self):
        super(double_supertrend_strategy, self).__init__()
        self._atr_period1 = self.Param("ATRPeriod1", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Period", "ATR period of the first Supertrend", "Supertrend 1")
        self._factor1 = self.Param("Factor1", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Factor", "Multiplier of the first Supertrend", "Supertrend 1")
        self._atr_period2 = self.Param("ATRPeriod2", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Period", "ATR period of the second Supertrend", "Supertrend 2")
        self._factor2 = self.Param("Factor2", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Factor", "Multiplier of the second Supertrend", "Supertrend 2")
        self._direction = self.Param("Direction", "Long") \
            .SetDisplay("Direction", "Allowed trade direction: Long, Short or Both", "Trading")
        self._tp_type = self.Param("TPType", "Supertrend") \
            .SetDisplay("TP Type", "Take-profit type: Supertrend or Percent", "Risk")
        self._tp_percent = self.Param("TPPercent", 1.5) \
            .SetNotNegative() \
            .SetDisplay("TP %", "Take-profit percentage for the Percent type", "Risk")
        self._sl_percent = self.Param("SLPercent", 10.0) \
            .SetNotNegative() \
            .SetDisplay("SL %", "Stop-loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._prev_above_both = None
        self._prev_below_both = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(double_supertrend_strategy, self).OnReseted()
        self._prev_above_both = None
        self._prev_below_both = None

    def OnStarted2(self, time):
        super(double_supertrend_strategy, self).OnStarted2(time)

        self._prev_above_both = None
        self._prev_below_both = None

        st1 = SuperTrend()
        st1.Length = self._atr_period1.Value
        st1.Multiplier = Decimal(self._factor1.Value)
        st2 = SuperTrend()
        st2.Length = self._atr_period2.Value
        st2.Multiplier = Decimal(self._factor2.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(st1, st2, self._process_candle).Start()

        tp_percent = float(self._tp_percent.Value)
        sl_percent = float(self._sl_percent.Value)
        take_profit = Unit(Decimal(tp_percent), UnitTypes.Percent) \
            if str(self._tp_type.Value).lower() == "percent" and tp_percent > 0 else Unit()
        stop_loss = Unit(Decimal(sl_percent), UnitTypes.Percent) if sl_percent > 0 else Unit()
        self.StartProtection(take_profit, stop_loss, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st1)
            self.DrawIndicator(area, st2)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, st1_value, st2_value):
        if candle.State != CandleStates.Finished:
            return

        if not st1_value.IsFormed or not st2_value.IsFormed:
            return

        line1 = float(st1_value.GetValue[Decimal](None))
        line2 = float(st2_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        above_both = close > line1 and close > line2
        below_both = close < line1 and close < line2

        prev_above = self._prev_above_both
        prev_below = self._prev_below_both
        self._prev_above_both = above_both
        self._prev_below_both = below_both

        if prev_above is None or prev_below is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        direction = str(self._direction.Value).lower()
        allow_long = direction != "short"
        allow_short = direction != "long"
        trail_on_second = str(self._tp_type.Value).lower() == "supertrend"

        long_signal = allow_long and above_both and not prev_above
        short_signal = allow_short and below_both and not prev_below

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and (close < line1 or (trail_on_second and close < line2)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close > line1 or (trail_on_second and close > line2)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return double_supertrend_strategy()
