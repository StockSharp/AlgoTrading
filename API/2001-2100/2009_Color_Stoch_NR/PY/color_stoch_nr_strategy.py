import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class color_stoch_nr_strategy(Strategy):
    """Stochastic oscillator with five entry modes and percentage protection."""

    # Numeric modes match ColorStochNrStrategy.AlgModes enum ordering in C#.
    BREAKDOWN = 0
    OSC_TWIST = 1
    SIGNAL_TWIST = 2
    OSC_DISPOSITION = 3
    SIGNAL_BREAKDOWN = 4

    def __init__(self):
        super(color_stoch_nr_strategy, self).__init__()
        self._mode = self.Param("Mode", self.OSC_DISPOSITION) \
            .SetDisplay("Algorithm Mode", "0=Breakdown, 1=OscTwist, 2=SignalTwist, 3=OscDisposition, 4=SignalBreakdown", "Parameters")
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("K Period", "Length for %K line", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Length for %D line", "Stochastic")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_k = None
        self._prev_d = None
        self._prev_k_delta = None
        self._prev_d_delta = None

    @property
    def Mode(self):
        return self._mode.Value

    @Mode.setter
    def Mode(self, value):
        self._mode.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(color_stoch_nr_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None
        self._prev_k_delta = None
        self._prev_d_delta = None

    def OnStarted2(self, time):
        super(color_stoch_nr_strategy, self).OnStarted2(time)
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._k_period.Value
        stochastic.D.Length = self._d_period.Value
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self.on_process).Start()

        self.StartProtection(
            stopLoss=Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            takeProfit=Unit(self._take_profit_percent.Value, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def on_process(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if stoch_value.K is None or stoch_value.D is None:
            return

        k = float(stoch_value.K)
        d = float(stoch_value.D)
        if self._prev_k is None or self._prev_d is None:
            self._prev_k = k
            self._prev_d = d
            return

        delta_k = k - self._prev_k
        delta_d = d - self._prev_d
        mode = int(self.Mode)
        buy = sell = False

        if mode == self.BREAKDOWN:
            buy = self._prev_k <= 50 < k
            sell = self._prev_k >= 50 > k
        elif mode == self.OSC_TWIST:
            if self._prev_k_delta is not None:
                buy = self._prev_k_delta <= 0 < delta_k
                sell = self._prev_k_delta >= 0 > delta_k
        elif mode == self.SIGNAL_TWIST:
            if self._prev_d_delta is not None:
                buy = self._prev_d_delta <= 0 < delta_d
                sell = self._prev_d_delta >= 0 > delta_d
        elif mode == self.OSC_DISPOSITION:
            buy = self._prev_k <= self._prev_d and k > d
            sell = self._prev_k >= self._prev_d and k < d
        elif mode == self.SIGNAL_BREAKDOWN:
            buy = self._prev_d <= 50 < d
            sell = self._prev_d >= 50 > d
        else:
            raise ValueError("Unsupported stochastic signal mode: %s" % mode)

        position = self.Position
        if buy and position <= 0:
            if position < 0:
                self.BuyMarket(-position)
            self.BuyMarket(self.Volume)
        elif sell and position >= 0:
            if position > 0:
                self.SellMarket(position)
            self.SellMarket(self.Volume)

        self._prev_k_delta = delta_k
        self._prev_d_delta = delta_d
        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return color_stoch_nr_strategy()
