import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class corrected_average_breakout_strategy(Strategy):
    """
    Strategy based on the Corrected Average breakout.
    Monitors price relative to a corrected moving average and trades on breakouts.
    """

    def __init__(self):
        super(corrected_average_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Period of moving average", "Indicator")
        self._level_points = self.Param("LevelPoints", 300) \
            .SetDisplay("Level Points", "Breakout distance in price steps", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss Points", "Stop loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit Points", "Take profit in price steps", "Risk")

        self._prev_corrected = 0.0
        self._prev_prev_corrected = 0.0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._is_initialized = False
        self._level = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(corrected_average_breakout_strategy, self).OnReseted()
        self._prev_corrected = 0.0
        self._prev_prev_corrected = 0.0
        self._prev_close = 0.0
        self._prev_prev_close = 0.0
        self._is_initialized = False
        self._level = 0.0

    def OnStarted(self, time):
        super(corrected_average_breakout_strategy, self).OnStarted(time)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        self._level = self._level_points.Value * step

        ma = ExponentialMovingAverage()
        ma.Length = self._length.Value
        std = StandardDeviation()
        std.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, std, self.on_process).Start()

        sl_dist = self._stop_loss_points.Value * step
        tp_dist = self._take_profit_points.Value * step
        self.StartProtection(
            Unit(float(tp_dist), UnitTypes.Absolute),
            Unit(float(sl_dist), UnitTypes.Absolute)
        )

    def on_process(self, candle, ma_val, std_val):
        if candle.State != CandleStates.Finished:
            return

        ma_val = float(ma_val)
        std_val = float(std_val)

        if not self._is_initialized:
            corrected = ma_val
            self._is_initialized = True
        else:
            v1 = std_val * std_val
            diff = self._prev_corrected - ma_val
            v2 = diff * diff
            if v2 < v1 or v2 == 0:
                k = 0.0
            else:
                k = 1.0 - (v1 / v2)
            corrected = self._prev_corrected + k * (ma_val - self._prev_corrected)

        buy_signal = (self._prev_prev_close > self._prev_prev_corrected + self._level and
                      self._prev_close <= self._prev_corrected + self._level)
        sell_signal = (self._prev_prev_close < self._prev_prev_corrected - self._level and
                       self._prev_close >= self._prev_corrected - self._level)

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_corrected = self._prev_corrected
        self._prev_prev_close = self._prev_close
        self._prev_corrected = corrected
        self._prev_close = float(candle.ClosePrice)

    def CreateClone(self):
        return corrected_average_breakout_strategy()
