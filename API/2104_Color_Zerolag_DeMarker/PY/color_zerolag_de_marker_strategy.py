import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_de_marker_strategy(Strategy):
    def __init__(self):
        super(color_zerolag_de_marker_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast Period", "Fast DeMarker period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow DeMarker period", "Indicator")
        self._smoothing = self.Param("Smoothing", 15) \
            .SetDisplay("Smoothing", "Smoothing factor for slow line", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._smooth_const = 0.0

    @property
    def fast_period(self):
        return self._fast_period.Value
    @property
    def slow_period(self):
        return self._slow_period.Value
    @property
    def smoothing(self):
        return self._smoothing.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_zerolag_de_marker_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._smooth_const = 0.0

    def OnStarted(self, time):
        super(color_zerolag_de_marker_strategy, self).OnStarted(time)
        s = float(self.smoothing)
        self._smooth_const = (s - 1.0) / s
        fast = DeMarker()
        fast.Length = self.fast_period
        slow = DeMarker()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_line = float(fast_val)
        s = float(self.smoothing)
        slow_line = fast_line / s + self._prev_slow * self._smooth_const

        if self._has_prev:
            if self._prev_fast < self._prev_slow and fast_line > slow_line and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_fast > self._prev_slow and fast_line < slow_line and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_fast = fast_line
        self._prev_slow = slow_line
        self._has_prev = True

    def CreateClone(self):
        return color_zerolag_de_marker_strategy()
