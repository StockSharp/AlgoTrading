import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class matrix_machine_learning_strategy(Strategy):
    """
    Matrix Machine Learning: Hopfield neural network on price direction sequences.
    Simplified Python version using momentum-based prediction.
    """

    def __init__(self):
        super(matrix_machine_learning_strategy, self).__init__()
        self._history_depth = self.Param("HistoryDepth", 120).SetDisplay("History", "Closes stored for network", "ML")
        self._predictor_length = self.Param("PredictorLength", 20).SetDisplay("Predictor", "Input vector length", "ML")
        self._forecast_length = self.Param("ForecastLength", 10).SetDisplay("Forecast", "Output vector length", "ML")
        self._cooldown_bars = self.Param("CooldownBars", 5).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))).SetDisplay("Candle Type", "Candles", "General")

        self._closes = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(matrix_machine_learning_strategy, self).OnReseted()
        self._closes = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(matrix_machine_learning_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._closes.append(close)
        hd = self._history_depth.Value
        if len(self._closes) > hd:
            self._closes = self._closes[-hd:]
        pl = self._predictor_length.Value
        fl = self._forecast_length.Value
        if len(self._closes) < pl + fl + 1:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        diffs = []
        for i in range(len(self._closes) - 1):
            diffs.append(1.0 if self._closes[i + 1] >= self._closes[i] else -1.0)
        if len(diffs) < pl:
            return
        recent = diffs[-pl:]
        direction = sum(recent)
        if direction > 0 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif direction < 0 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return matrix_machine_learning_strategy()
