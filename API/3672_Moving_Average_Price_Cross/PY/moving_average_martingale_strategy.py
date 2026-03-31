import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_average_martingale_strategy(Strategy):
    def __init__(self):
        super(moving_average_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._ma_period = self.Param("MaPeriod", 50)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 2)

        self._prev_close = None
        self._prev_ma = None
        self._curr_close = None
        self._curr_ma = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnReseted(self):
        super(moving_average_martingale_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ma = None
        self._curr_close = None
        self._curr_ma = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(moving_average_martingale_strategy, self).OnStarted2(time)
        self._prev_close = None
        self._prev_ma = None
        self._curr_close = None
        self._curr_ma = None
        self._cooldown_remaining = 0

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma_val = float(ma_value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._curr_close is None:
            self._curr_close = close
            self._curr_ma = ma_val
            return

        if self._prev_close is None:
            self._prev_close = self._curr_close
            self._prev_ma = self._curr_ma
            self._curr_close = close
            self._curr_ma = ma_val
            return

        prev_close = self._prev_close
        prev_ma = self._prev_ma
        curr_close = self._curr_close
        curr_ma = self._curr_ma

        # MA crosses above price -> sell; MA crosses below price -> buy
        crossed_below_price = prev_ma < prev_close and curr_ma > curr_close
        crossed_above_price = prev_ma > prev_close and curr_ma < curr_close

        if self.Position == 0 and self._cooldown_remaining == 0:
            if crossed_below_price:
                self.SellMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif crossed_above_price:
                self.BuyMarket()
                self._cooldown_remaining = self.SignalCooldownBars

        self._prev_close = curr_close
        self._prev_ma = curr_ma
        self._curr_close = close
        self._curr_ma = ma_val

    def CreateClone(self):
        return moving_average_martingale_strategy()
