import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class moving_average_entanglement_strategy(Strategy):
    """
    Moving average entanglement: fast/slow SMA gap vs ATR dead zone filter.
    """

    def __init__(self):
        super(moving_average_entanglement_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 3).SetDisplay("Fast MA", "Fast SMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 14).SetDisplay("Slow MA", "Slow SMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 10).SetDisplay("ATR Length", "ATR length", "Indicators")
        self._dead_zone_pct = self.Param("DeadZonePercentage", 40.0).SetDisplay("Dead Zone %", "ATR dead zone %", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_buy = False
        self._prev_sell = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_entanglement_strategy, self).OnReseted()
        self._prev_buy = False
        self._prev_sell = False

    def OnStarted2(self, time):
        super(moving_average_entanglement_strategy, self).OnStarted2(time)
        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)
        gapping = abs(fast - slow)
        dead_zone = atr * float(self._dead_zone_pct.Value) * 0.01
        buy_cond = fast > slow and gapping > dead_zone
        sell_cond = fast < slow and gapping > dead_zone
        if buy_cond and not self._prev_buy and self.Position <= 0:
            self.BuyMarket()
        elif sell_cond and not self._prev_sell and self.Position >= 0:
            self.SellMarket()
        self._prev_buy = buy_cond
        self._prev_sell = sell_cond

    def CreateClone(self):
        return moving_average_entanglement_strategy()
