import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import LinearReg
from StockSharp.Algo.Strategies import Strategy


class lsma_angle_strategy(Strategy):
    def __init__(self):
        super(lsma_angle_strategy, self).__init__()
        self._lsma_period = self.Param("LsmaPeriod", 25) \
            .SetDisplay("LSMA Period", "LSMA calculation length", "Indicator")
        self._slope_threshold = self.Param("SlopeThreshold", 0.05) \
            .SetDisplay("Slope Threshold", "Percentage slope threshold", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_lsma = None
        self._prev_slope = 0.0

    @property
    def lsma_period(self):
        return self._lsma_period.Value
    @property
    def slope_threshold(self):
        return self._slope_threshold.Value
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
        super(lsma_angle_strategy, self).OnReseted()
        self._prev_lsma = None
        self._prev_slope = 0.0

    def OnStarted2(self, time):
        super(lsma_angle_strategy, self).OnStarted2(time)
        lsma = LinearReg()
        lsma.Length = self.lsma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lsma, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lsma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, lsma_value):
        if candle.State != CandleStates.Finished:
            return
        lsma_value = float(lsma_value)
        if self._prev_lsma is None:
            self._prev_lsma = lsma_value
            return

        slope = (lsma_value - self._prev_lsma) / self._prev_lsma * 100.0 if self._prev_lsma != 0 else 0.0
        threshold = float(self.slope_threshold)

        was_up = self._prev_slope > threshold
        was_down = self._prev_slope < -threshold
        is_up = slope > threshold
        is_down = slope < -threshold

        if not was_up and is_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif not was_down and is_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif was_up and not is_up and self.Position > 0:
            self.SellMarket()
        elif was_down and not is_down and self.Position < 0:
            self.BuyMarket()

        self._prev_slope = slope
        self._prev_lsma = lsma_value

    def CreateClone(self):
        return lsma_angle_strategy()
