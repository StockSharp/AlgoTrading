import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import LinearReg
from StockSharp.Algo.Strategies import Strategy


class linear_regression_slope_v1_strategy(Strategy):
    def __init__(self):
        super(linear_regression_slope_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Bars for regression", "Parameters")
        self._trigger_shift = self.Param("TriggerShift", 1) \
            .SetDisplay("Trigger Shift", "Lag for trigger line", "Parameters")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._slope_history = []
        self._filled = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def length(self):
        return self._length.Value
    @property
    def trigger_shift(self):
        return self._trigger_shift.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    def OnReseted(self):
        super(linear_regression_slope_v1_strategy, self).OnReseted()
        self._slope_history = []
        self._filled = 0

    def OnStarted(self, time):
        super(linear_regression_slope_v1_strategy, self).OnStarted(time)
        max_len = int(self.trigger_shift) + 3
        self._slope_history = [0.0] * max_len
        self._filled = 0
        slope = LinearReg()
        slope.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(slope, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, slope)
            self.DrawOwnTrades(area)

    def _shift(self, value):
        for i in range(len(self._slope_history) - 1):
            self._slope_history[i] = self._slope_history[i + 1]
        self._slope_history[-1] = value
        if self._filled < len(self._slope_history):
            self._filled += 1

    def process_candle(self, candle, slope_val):
        if candle.State != CandleStates.Finished:
            return
        slope_val = float(slope_val)
        self._shift(slope_val)
        if self._filled < len(self._slope_history):
            return
        n = len(self._slope_history)
        s2 = self._slope_history[n - 3]
        s1 = self._slope_history[n - 2]
        t2 = self._slope_history[0]
        t1 = self._slope_history[1]

        if s2 > t2:
            if self.Position < 0:
                self.BuyMarket()
            if s1 <= t1 and self.Position <= 0:
                self.BuyMarket()
        elif t2 > s2:
            if self.Position > 0:
                self.SellMarket()
            if t1 <= s1 and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return linear_regression_slope_v1_strategy()
