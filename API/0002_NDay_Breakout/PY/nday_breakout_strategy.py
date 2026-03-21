import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class nday_breakout_strategy(Strategy):
    """
    N-day high/low breakout strategy.
    Enters long when price breaks above the N-day high.
    Enters short when price breaks below the N-day low.
    """

    def __init__(self):
        super(nday_breakout_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 1500).SetDisplay("Lookback Period", "Number of bars to determine the high/low range", "Strategy Parameters")
        self._ma_period = self.Param("MaPeriod", 300).SetDisplay("MA Period", "Period for the moving average used as exit signal", "Strategy Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        self._n_day_high = 0.0
        self._n_day_low = float('inf')
        self._is_formed = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(nday_breakout_strategy, self).OnReseted()
        self._n_day_high = 0.0
        self._n_day_low = float('inf')
        self._is_formed = False

    def OnStarted(self, time):
        super(nday_breakout_strategy, self).OnStarted(time)

        self._highest = Highest()
        self._highest.Length = self._lookback_period.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback_period.Value
        self._ma = SimpleMovingAverage()
        self._ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._lowest, self._ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawIndicator(area, self._lowest)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_val, lowest_val, ma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._is_formed:
            if self._highest.IsFormed and self._lowest.IsFormed:
                self._n_day_high = float(highest_val)
                self._n_day_low = float(lowest_val)
                self._is_formed = True
            return

        h = float(highest_val)
        l = float(lowest_val)

        if float(candle.HighPrice) > self._n_day_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif float(candle.LowPrice) < self._n_day_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

        self._n_day_high = h
        self._n_day_low = l

    def CreateClone(self):
        return nday_breakout_strategy()
