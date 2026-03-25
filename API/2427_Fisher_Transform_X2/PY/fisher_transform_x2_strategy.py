import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import EhlersFisherTransform, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class fisher_transform_x2_strategy(Strategy):
    """
    Fisher Transform X2: Dual Fisher indicator strategy.
    Trend Fisher defines direction; Signal Fisher generates entries.
    Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(fisher_transform_x2_strategy, self).__init__()
        self._trend_length = self.Param("TrendLength", 40) \
            .SetDisplay("Trend Length", "Fisher length for trend", "Parameters")
        self._signal_length = self.Param("SignalLength", 20) \
            .SetDisplay("Signal Length", "Fisher length for signal", "Parameters")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._trend_fisher = None
        self._prev_trend = 0.0
        self._prev_signal = 0.0
        self._prev_prev_signal = 0.0
        self._trend_direction = 0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fisher_transform_x2_strategy, self).OnReseted()
        self._prev_trend = 0.0
        self._prev_signal = 0.0
        self._prev_prev_signal = 0.0
        self._trend_direction = 0
        self._count = 0

    def OnStarted(self, time):
        super(fisher_transform_x2_strategy, self).OnStarted(time)

        self._trend_fisher = EhlersFisherTransform()
        self._trend_fisher.Length = self._trend_length.Value
        self.Indicators.Add(self._trend_fisher)

        signal_fisher = EhlersFisherTransform()
        signal_fisher.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(signal_fisher, self._process_candle).Start()

        tp = self._take_profit.Value
        sl = self._stop_loss.Value
        self.StartProtection(
            Unit(tp, UnitTypes.Absolute),
            Unit(sl, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, signal_fisher)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, signal_result):
        if candle.State != CandleStates.Finished:
            return

        trend_input = CandleIndicatorValue(self._trend_fisher, candle)
        trend_input.IsFinal = True
        trend_result = self._trend_fisher.Process(trend_input)
        if not self._trend_fisher.IsFormed:
            return

        signal_main = signal_result.MainLine
        trend_main = trend_result.MainLine

        signal_val = float(signal_main) if signal_main is not None else 0.0
        trend_val = float(trend_main) if trend_main is not None else 0.0

        self._count += 1
        if self._count < 3:
            self._prev_prev_signal = self._prev_signal
            self._prev_signal = signal_val
            self._prev_trend = trend_val
            return

        if trend_val > self._prev_trend:
            self._trend_direction = 1
        elif trend_val < self._prev_trend:
            self._trend_direction = -1

        signal_cross_up = signal_val > self._prev_signal and self._prev_signal <= self._prev_prev_signal and signal_val < 0
        signal_cross_down = signal_val < self._prev_signal and self._prev_signal >= self._prev_prev_signal and signal_val > 0

        if self._trend_direction > 0 and signal_cross_up and self.Position <= 0:
            self.BuyMarket()
        elif self._trend_direction < 0 and signal_cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_trend = trend_val
        self._prev_prev_signal = self._prev_signal
        self._prev_signal = signal_val

    def CreateClone(self):
        return fisher_transform_x2_strategy()
