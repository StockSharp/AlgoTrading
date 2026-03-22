import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_mean_reversion_strategy(Strategy):
    """
    CCI Mean Reversion strategy.
    Enters positions when CCI is significantly below or above its average value.
    """

    def __init__(self):
        super(cci_mean_reversion_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "Indicators")
        self._average_period = self.Param("AveragePeriod", 20) \
            .SetDisplay("Average Period", "Period for calculating CCI average and standard deviation", "Settings")
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        self._prev_cci = 0.0
        self._avg_cci = 0.0
        self._std_dev_cci = 0.0
        self._sum_cci = 0.0
        self._sum_squares_cci = 0.0
        self._count = 0
        self._cci_values = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_mean_reversion_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._avg_cci = 0.0
        self._std_dev_cci = 0.0
        self._sum_cci = 0.0
        self._sum_squares_cci = 0.0
        self._count = 0
        self._cci_values = []

    def OnStarted(self, time):
        super(cci_mean_reversion_strategy, self).OnStarted(time)

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self._stop_loss_percent.Value, UnitTypes.Percent)
        )

    def on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        current_cci = float(cci_value)
        self._update_cci_statistics(current_cci)
        self._prev_cci = current_cci

        if self._count < self._average_period.Value:
            return

        if self.Position == 0:
            if current_cci < self._avg_cci - self._deviation_multiplier.Value * self._std_dev_cci:
                self.BuyMarket(self.Volume)
            elif current_cci > self._avg_cci + self._deviation_multiplier.Value * self._std_dev_cci:
                self.SellMarket(self.Volume)
        elif self.Position > 0:
            if current_cci > self._avg_cci:
                self.ClosePosition()
        elif self.Position < 0:
            if current_cci < self._avg_cci:
                self.ClosePosition()

    def _update_cci_statistics(self, current_cci):
        self._cci_values.append(current_cci)
        self._sum_cci += current_cci
        self._sum_squares_cci += current_cci * current_cci
        self._count += 1

        if len(self._cci_values) > self._average_period.Value:
            oldest_cci = self._cci_values.pop(0)
            self._sum_cci -= oldest_cci
            self._sum_squares_cci -= oldest_cci * oldest_cci
            self._count -= 1

        if self._count > 0:
            self._avg_cci = self._sum_cci / self._count
            if self._count > 1:
                variance = (self._sum_squares_cci - (self._sum_cci * self._sum_cci) / self._count) / (self._count - 1)
                self._std_dev_cci = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_cci = 0

    def CreateClone(self):
        return cci_mean_reversion_strategy()
