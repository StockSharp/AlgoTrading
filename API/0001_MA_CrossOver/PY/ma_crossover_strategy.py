import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_crossover_strategy(Strategy):
    """
    Moving average crossover strategy.
    Enters long when fast MA crosses above slow MA.
    Enters short when fast MA crosses below slow MA.
    """

    def __init__(self):
        super(ma_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 100).SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
        self._slow_length = self.Param("SlowLength", 400).SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._entry_price = 0.0
        self._is_long_position = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_crossover_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._is_long_position = False

    def OnStarted2(self, time):
        super(ma_crossover_strategy, self).OnStarted2(time)

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self._fast_length.Value
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self._slow_length.Value

        self._was_fast_less = False
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if not self._is_initialized:
            self._was_fast_less = fv < sv
            self._is_initialized = True
            return

        is_fast_less = fv < sv

        if self._was_fast_less != is_fast_less:
            if not is_fast_less:
                if self.Position <= 0:
                    self._entry_price = float(candle.ClosePrice)
                    self._is_long_position = True
                    self.BuyMarket()
            else:
                if self.Position >= 0:
                    self._entry_price = float(candle.ClosePrice)
                    self._is_long_position = False
                    self.SellMarket()
            self._was_fast_less = is_fast_less

    def CreateClone(self):
        return ma_crossover_strategy()
