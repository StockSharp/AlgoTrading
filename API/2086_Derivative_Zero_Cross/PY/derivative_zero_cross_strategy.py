import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class derivative_zero_cross_strategy(Strategy):
    def __init__(self):
        super(derivative_zero_cross_strategy, self).__init__()
        self._derivative_period = self.Param("DerivativePeriod", 14) \
            .SetDisplay("Derivative Period", "Smoothing period for derivative", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_derivative = None

    @property
    def derivative_period(self):
        return self._derivative_period.Value

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
        super(derivative_zero_cross_strategy, self).OnReseted()
        self._prev_derivative = None

    def OnStarted(self, time):
        super(derivative_zero_cross_strategy, self).OnStarted(time)
        momentum = Momentum()
        momentum.Length = self.derivative_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, mom_value):
        if candle.State != CandleStates.Finished:
            return
        mom_value = float(mom_value)
        derivative = mom_value / float(self.derivative_period) * 100.0

        if self._prev_derivative is None:
            self._prev_derivative = derivative
            return

        prev = self._prev_derivative

        if prev <= 0.0 and derivative > 0.0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif prev >= 0.0 and derivative < 0.0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_derivative = derivative

    def CreateClone(self):
        return derivative_zero_cross_strategy()
