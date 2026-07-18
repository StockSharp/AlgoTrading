import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class beginner_breakout_strategy(Strategy):
    # TrendDirections: 0=None, 1=Up, 2=Down
    def __init__(self):
        super(beginner_breakout_strategy, self).__init__()
        self._period = self.Param("Period", 9) \
            .SetDisplay("Period", "Lookback period for highs/lows", "General")
        self._shift_percent = self.Param("ShiftPercent", 30.0) \
            .SetDisplay("Shift %", "Percentage shift from channel", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss", "Stop loss in percent", "Risk")
        self._take_profit = self.Param("TakeProfit", 4.0) \
            .SetDisplay("Take Profit", "Take profit in percent", "Risk")
        self._trend = 0  # 0=None, 1=Up, 2=Down

    @property
    def period(self):
        return self._period.Value

    @property
    def shift_percent(self):
        return self._shift_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    def OnReseted(self):
        super(beginner_breakout_strategy, self).OnReseted()
        self._trend = 0

    def OnStarted2(self, time):
        super(beginner_breakout_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.period
        lowest = Lowest()
        lowest.Length = self.period
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss), UnitTypes.Percent))
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return
        high_value = float(high_value)
        low_value = float(low_value)
        shift = float(self.shift_percent)
        rng = (high_value - low_value) * shift / 100.0
        close = float(candle.ClosePrice)
        if self._trend != 2 and close <= low_value + rng:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._trend = 2
        elif self._trend != 1 and close >= high_value - rng:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._trend = 1

    def CreateClone(self):
        return beginner_breakout_strategy()
