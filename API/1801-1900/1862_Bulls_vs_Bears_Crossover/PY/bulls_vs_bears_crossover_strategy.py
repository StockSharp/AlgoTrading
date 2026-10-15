import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy

class bulls_vs_bears_crossover_strategy(Strategy):
    """
    Bulls vs Bears crossover strategy.
    Opens long or short positions when the distance from high and low to a moving average crosses.
    """

    def __init__(self):
        super(bulls_vs_bears_crossover_strategy, self).__init__()

        self._ma_type = self.Param("MaType", 0) \
            .SetDisplay("MA Type", "0=SMA,1=EMA,2=SMMA,3=WMA", "General")
        self._ma_length = self.Param("MaLength", 12) \
            .SetDisplay("MA Length", "Moving average period", "General")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Loss in price steps", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Profit in price steps", "Risk")
        self._open_long = self.Param("OpenLong", True) \
            .SetDisplay("Open Long", "Allow long entries", "General")
        self._open_short = self.Param("OpenShort", True) \
            .SetDisplay("Open Short", "Allow short entries", "General")
        self._close_long = self.Param("CloseLong", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "General")
        self._close_short = self.Param("CloseShort", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe to process", "General")
        self._min_spread_steps = self.Param("MinSpreadSteps", 60.0) \
            .SetDisplay("Minimum Spread", "Minimum spread between bull and bear power in price steps", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")

        self._ma = None
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def ma_type(self):
        return self._ma_type.Value

    @ma_type.setter
    def ma_type(self, value):
        self._ma_type.Value = value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @ma_length.setter
    def ma_length(self, value):
        self._ma_length.Value = value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @take_profit.setter
    def take_profit(self, value):
        self._take_profit.Value = value

    @property
    def open_long(self):
        return self._open_long.Value

    @open_long.setter
    def open_long(self, value):
        self._open_long.Value = value

    @property
    def open_short(self):
        return self._open_short.Value

    @open_short.setter
    def open_short(self, value):
        self._open_short.Value = value

    @property
    def close_long(self):
        return self._close_long.Value

    @close_long.setter
    def close_long(self, value):
        self._close_long.Value = value

    @property
    def close_short(self):
        return self._close_short.Value

    @close_short.setter
    def close_short(self, value):
        self._close_short.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def min_spread_steps(self):
        return self._min_spread_steps.Value

    @min_spread_steps.setter
    def min_spread_steps(self, value):
        self._min_spread_steps.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(bulls_vs_bears_crossover_strategy, self).OnReseted()
        self._ma = None
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def _create_ma(self, ma_type, length):
        if ma_type == 1:
            ma = ExponentialMovingAverage()
        elif ma_type == 2:
            ma = SmoothedMovingAverage()
        elif ma_type == 3:
            ma = WeightedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(bulls_vs_bears_crossover_strategy, self).OnStarted2(time)

        self._ma = self._create_ma(self.ma_type, self.ma_length)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

        self.StartProtection(None, None)

    def on_process(self, candle, ma_value):
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        bull = (candle.HighPrice - ma_value) / step
        bear = (ma_value - candle.LowPrice) / step

        if candle.State != CandleStates.Finished or not self._ma.IsFormed:
            self._prev_bull = bull
            self._prev_bear = bear
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        spread = abs(bull - bear)
        cross_down = self._prev_bull > self._prev_bear and bull <= bear and spread >= self.min_spread_steps
        cross_up = self._prev_bull < self._prev_bear and bull >= bear and spread >= self.min_spread_steps

        if self._cooldown_remaining == 0:
            if cross_down:
                if self.close_short and self.Position < 0:
                    self.BuyMarket()
                if self.open_long and self.Position <= 0:
                    self.BuyMarket()
                    self._entry_price = candle.ClosePrice
                    self._cooldown_remaining = self.cooldown_bars
            elif cross_up:
                if self.close_long and self.Position > 0:
                    self.SellMarket()
                if self.open_short and self.Position >= 0:
                    self.SellMarket()
                    self._entry_price = candle.ClosePrice
                    self._cooldown_remaining = self.cooldown_bars

        if self.Position > 0:
            tp = self._entry_price + self.take_profit * step
            sl = self._entry_price - self.stop_loss * step
            if candle.ClosePrice >= tp or candle.ClosePrice <= sl:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0:
            tp = self._entry_price - self.take_profit * step
            sl = self._entry_price + self.stop_loss * step
            if candle.ClosePrice <= tp or candle.ClosePrice >= sl:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars

        self._prev_bull = bull
        self._prev_bear = bear

    def CreateClone(self):
        return bulls_vs_bears_crossover_strategy()
