import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class id_emarsi_on_chart_strategy(Strategy):
    """
    RSI with EMA crossover strategy. Enters when RSI crosses
    its own EMA (calculated manually via exponential smoothing).
    """

    def __init__(self):
        super(id_emarsi_on_chart_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 16) \
            .SetDisplay("RSI Length", "RSI length", "General")
        self._ema_length = self.Param("EmaLength", 42) \
            .SetDisplay("EMA Length", "EMA of RSI length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type", "General")

        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(id_emarsi_on_chart_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(id_emarsi_on_chart_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_val)

        alpha = 2.0 / (self._ema_length.Value + 1.0)
        if not self._is_initialized:
            ema = rsi
        else:
            ema = self._prev_ema * (1.0 - alpha) + rsi * alpha

        if not self._is_initialized:
            self._prev_rsi = rsi
            self._prev_ema = ema
            self._is_initialized = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi
            self._prev_ema = ema
            return

        cross_up = self._prev_rsi <= self._prev_ema and rsi > ema
        cross_down = self._prev_rsi >= self._prev_ema and rsi < ema

        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

        self._prev_rsi = rsi
        self._prev_ema = ema

    def CreateClone(self):
        return id_emarsi_on_chart_strategy()
