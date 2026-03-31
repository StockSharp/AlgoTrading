import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_strategy(Strategy):

    def __init__(self):
        super(trailing_stop_strategy, self).__init__()

        self._take_profit = self.Param("TakeProfit", 3500.0) \
            .SetDisplay("Take Profit", "Profit distance in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 1200.0) \
            .SetDisplay("Stop Loss", "Loss distance in price units", "Risk")
        self._trailing = self.Param("Trailing", 800.0) \
            .SetDisplay("Trailing", "Trailing stop distance", "Risk")
        self._fast_ma_period = self.Param("FastMaPeriod", 6) \
            .SetDisplay("Fast MA", "Fast moving average period", "Indicator")
        self._slow_ma_period = self.Param("SlowMaPeriod", 18) \
            .SetDisplay("Slow MA", "Slow moving average period", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for price updates", "General")

        self._fast_ma = None
        self._slow_ma = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_fast_ma = 0.0
        self._prev_slow_ma = 0.0
        self._is_initialized = False
        self._bars_since_exit = 0

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def Trailing(self):
        return self._trailing.Value

    @Trailing.setter
    def Trailing(self, value):
        self._trailing.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(trailing_stop_strategy, self).OnStarted2(time)

        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self.FastMaPeriod
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod

        self.Indicators.Add(self._fast_ma)
        self.Indicators.Add(self._slow_ma)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = candle.ClosePrice
        fi = DecimalIndicatorValue(self._fast_ma, price, candle.OpenTime)
        fi.IsFinal = True
        fast_value = float(self._fast_ma.Process(fi))
        si = DecimalIndicatorValue(self._slow_ma, price, candle.OpenTime)
        si.IsFinal = True
        slow_value = float(self._slow_ma.Process(si))

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        if not self._is_initialized:
            self._prev_fast_ma = fast_value
            self._prev_slow_ma = slow_value
            self._is_initialized = True
            return

        if self.Position != 0:
            self._prev_fast_ma = fast_value
            self._prev_slow_ma = slow_value
            return

        cross_up = self._prev_fast_ma <= self._prev_slow_ma and fast_value > slow_value
        cross_down = self._prev_fast_ma >= self._prev_slow_ma and fast_value < slow_value

        if cross_up:
            self.BuyMarket()
        elif cross_down:
            self.SellMarket()

        self._prev_fast_ma = fast_value
        self._prev_slow_ma = slow_value

    def OnReseted(self):
        super(trailing_stop_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_fast_ma = 0.0
        self._prev_slow_ma = 0.0
        self._is_initialized = False
        self._bars_since_exit = self.CooldownBars

    def CreateClone(self):
        return trailing_stop_strategy()
