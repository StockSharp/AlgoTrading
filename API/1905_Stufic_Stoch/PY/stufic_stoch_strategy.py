import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StochasticOscillator, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class stufic_stoch_strategy(Strategy):
    def __init__(self):
        super(stufic_stoch_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast moving average period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow moving average period", "Indicators")
        self._stoch_k_period = self.Param("StochKPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %K", "%K period for Stochastic", "Indicators")
        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %D", "%D period for Stochastic", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetDisplay("Overbought", "Overbought level", "Trading")
        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetDisplay("Oversold", "Oversold level", "Trading")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_ma = None
        self._slow_ma = None
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_first = True

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value
    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value
    @property
    def stoch_k_period(self):
        return self._stoch_k_period.Value
    @property
    def stoch_d_period(self):
        return self._stoch_d_period.Value
    @property
    def overbought_level(self):
        return self._overbought_level.Value
    @property
    def oversold_level(self):
        return self._oversold_level.Value
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stufic_stoch_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(stufic_stoch_strategy, self).OnStarted(time)
        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.slow_ma_period
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.stoch_k_period
        stochastic.D.Length = self.stoch_d_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()
        self.StartProtection(
            None,
            Unit(float(self.stop_loss_percent), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        fast_inp = DecimalIndicatorValue(self._fast_ma, candle.ClosePrice, candle.OpenTime)
        fast_inp.IsFinal = True
        fast_result = self._fast_ma.Process(fast_inp)
        slow_inp = DecimalIndicatorValue(self._slow_ma, candle.ClosePrice, candle.OpenTime)
        slow_inp.IsFinal = True
        slow_result = self._slow_ma.Process(slow_inp)
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return
        fast = float(fast_result)
        slow = float(slow_result)
        k_val = stoch_value.K
        d_val = stoch_value.D
        if k_val is None or d_val is None:
            return
        k = float(k_val)
        d = float(d_val)
        if self._is_first:
            self._prev_k = k
            self._prev_d = d
            self._is_first = False
            return
        ob = float(self.overbought_level)
        os_lvl = float(self.oversold_level)
        # Bullish: %K crosses above %D in oversold zone, trend up
        if self._prev_k <= self._prev_d and k > d and k < os_lvl and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Bearish: %K crosses below %D in overbought zone, trend down
        elif self._prev_k >= self._prev_d and k < d and k > ob and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return stufic_stoch_strategy()
