import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ang_zad_c_time_mm_recovery_strategy(Strategy):
    def __init__(self):
        super(ang_zad_c_time_mm_recovery_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ki = self.Param("Ki", 4.000001) \
            .SetDisplay("Ki", "Smoothing coefficient", "Indicator")

        self._has_state = False
        self._upper_line = 0.0
        self._lower_line = 0.0
        self._previous_price = 0.0
        self._prev_up = None
        self._prev_dn = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Ki(self):
        return self._ki.Value

    def OnReseted(self):
        super(ang_zad_c_time_mm_recovery_strategy, self).OnReseted()
        self._has_state = False
        self._upper_line = 0.0
        self._lower_line = 0.0
        self._previous_price = 0.0
        self._prev_up = None
        self._prev_dn = None

    def OnStarted2(self, time):
        super(ang_zad_c_time_mm_recovery_strategy, self).OnStarted2(time)
        self._has_state = False
        self._prev_up = None
        self._prev_dn = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _update_indicator(self, price):
        if not self._has_state:
            self._upper_line = price
            self._lower_line = price
            self._previous_price = price
            self._has_state = True
            return (self._upper_line, self._lower_line)

        ki = float(self.Ki)

        if price > self._upper_line and price > self._previous_price:
            self._upper_line += (price - self._upper_line) / ki

        if price < self._upper_line and price < self._previous_price:
            self._upper_line += (price - self._upper_line) / ki

        if price > self._lower_line and price < self._previous_price:
            self._lower_line += (price - self._lower_line) / ki

        if price < self._lower_line and price > self._previous_price:
            self._lower_line += (price - self._lower_line) / ki

        self._previous_price = price

        return (self._upper_line, self._lower_line)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        upper, lower = self._update_indicator(price)

        if self._prev_up is None or self._prev_dn is None:
            self._prev_up = upper
            self._prev_dn = lower
            return

        prev_up = self._prev_up
        prev_dn = self._prev_dn

        # Buy signal: previous upper was below lower, now crossing above
        if prev_up <= prev_dn and upper > lower:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Sell signal: previous upper was above lower, now crossing below
        elif prev_up >= prev_dn and upper < lower:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_up = upper
        self._prev_dn = lower

    def CreateClone(self):
        return ang_zad_c_time_mm_recovery_strategy()
