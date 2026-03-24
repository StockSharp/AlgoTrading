import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Lowest
from StockSharp.Algo.Strategies import Strategy


class vlt_trader_strategy(Strategy):
    def __init__(self):
        super(vlt_trader_strategy, self).__init__()
        self._period = self.Param("Period", 6) \
            .SetDisplay("Period", "Indicator period", "General")
        self._stop_loss = self.Param("StopLoss", 550.0) \
            .SetDisplay("Stop loss", "Stop loss in price steps", "Risk")
        self._take_profit_param = self.Param("TakeProfit", 550.0) \
            .SetDisplay("Take profit", "Take profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candles for calculation", "General")
        self._signal_life_bars = self.Param("SignalLifeBars", 3) \
            .SetDisplay("Signal Life Bars", "Number of bars to keep pending breakout signal", "General")

        self._prev_range = 0.0
        self._prev_min_range = None
        self._signal_high = 0.0
        self._signal_low = 0.0
        self._pending_breakout = False
        self._remaining_signal_bars = 0
        self._ranges = []

    @property
    def period(self):
        return self._period.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit_param.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def signal_life_bars(self):
        return self._signal_life_bars.Value

    def OnReseted(self):
        super(vlt_trader_strategy, self).OnReseted()
        self._prev_range = 0.0
        self._prev_min_range = None
        self._signal_high = 0.0
        self._signal_low = 0.0
        self._pending_breakout = False
        self._remaining_signal_bars = 0
        self._ranges = []

    def OnStarted(self, time):
        super(vlt_trader_strategy, self).OnStarted(time)
        self._prev_range = 0.0
        self._prev_min_range = None
        self._signal_high = 0.0
        self._signal_low = 0.0
        self._pending_breakout = False
        self._remaining_signal_bars = 0
        self._ranges = []

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0
        self.StartProtection(
            Unit(float(self.take_profit) * step, UnitTypes.Absolute),
            Unit(float(self.stop_loss) * step, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        range_val = float(candle.HighPrice) - float(candle.LowPrice)
        self._ranges.append(range_val)
        period = self.period
        if len(self._ranges) > period:
            self._ranges.pop(0)

        if len(self._ranges) < period:
            return

        min_range = min(self._ranges)

        # Check for pending breakout entry
        if self._pending_breakout and self.Position == 0:
            if float(candle.HighPrice) >= self._signal_high and float(candle.ClosePrice) > self._signal_high:
                self.BuyMarket()
                self._pending_breakout = False
                self._remaining_signal_bars = 0
            elif float(candle.LowPrice) <= self._signal_low and float(candle.ClosePrice) < self._signal_low:
                self.SellMarket()
                self._pending_breakout = False
                self._remaining_signal_bars = 0
            else:
                self._remaining_signal_bars -= 1
                if self._remaining_signal_bars <= 0:
                    self._pending_breakout = False

        # Detect low-volatility signal
        has_previous_range = self._prev_min_range is not None
        is_signal = (has_previous_range and
                     range_val <= min_range * 1.08 and
                     self._prev_range > self._prev_min_range * 1.05)

        self._prev_range = range_val
        self._prev_min_range = min_range

        if is_signal and self.Position == 0:
            self._signal_high = float(candle.HighPrice)
            self._signal_low = float(candle.LowPrice)
            self._pending_breakout = True
            self._remaining_signal_bars = self.signal_life_bars

    def CreateClone(self):
        return vlt_trader_strategy()
