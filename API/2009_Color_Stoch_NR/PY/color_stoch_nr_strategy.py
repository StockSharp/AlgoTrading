import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class color_stoch_nr_strategy(Strategy):
    """
    Strategy based on the stochastic oscillator with OscDisposition mode.
    Buys when %K crosses above %D, sells when %K crosses below %D.
    """

    def __init__(self):
        super(color_stoch_nr_strategy, self).__init__()
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

        self._prev_k = 0.0
        self._prev_d = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_stoch_nr_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0

    def OnStarted(self, time):
        super(color_stoch_nr_strategy, self).OnStarted(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._k_period.Value
        stochastic.D.Length = self._d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
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

        k = float(stoch_value.K) if stoch_value.K is not None else 0.0
        d = float(stoch_value.D) if stoch_value.D is not None else 0.0

        if k == 0.0 and d == 0.0:
            return

        # OscDisposition mode: K crosses D
        if self._prev_k <= self._prev_d and k > d and self.Position <= 0:

            self.BuyMarket()

        elif self._prev_k >= self._prev_d and k < d and self.Position >= 0:

            self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return color_stoch_nr_strategy()
