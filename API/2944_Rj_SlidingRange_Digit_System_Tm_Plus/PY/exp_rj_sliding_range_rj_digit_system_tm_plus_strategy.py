import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class exp_rj_sliding_range_rj_digit_system_tm_plus_strategy(Strategy):
    def __init__(self):
        super(exp_rj_sliding_range_rj_digit_system_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "Channel lookback", "Indicators")

        self._prev_upper = None
        self._prev_lower = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Period(self):
        return self._period.Value

    def OnReseted(self):
        super(exp_rj_sliding_range_rj_digit_system_tm_plus_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None

    def OnStarted(self, time):
        super(exp_rj_sliding_range_rj_digit_system_tm_plus_strategy, self).OnStarted(time)
        self._prev_upper = None
        self._prev_lower = None

        highest = Highest()
        highest.Length = self.Period
        lowest = Lowest()
        lowest.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, upper_value, lower_value):
        if candle.State != CandleStates.Finished:
            return

        uv = float(upper_value)
        lv = float(lower_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_upper = uv
            self._prev_lower = lv
            return

        close = float(candle.ClosePrice)

        if self._prev_upper is None or self._prev_lower is None:
            self._prev_upper = uv
            self._prev_lower = lv
            return

        # Breakout above previous upper
        if close > self._prev_upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Breakdown below previous lower
        elif close < self._prev_lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_upper = uv
        self._prev_lower = lv

    def CreateClone(self):
        return exp_rj_sliding_range_rj_digit_system_tm_plus_strategy()
