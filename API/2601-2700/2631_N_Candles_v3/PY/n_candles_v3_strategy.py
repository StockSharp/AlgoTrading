import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class n_candles_v3_strategy(Strategy):
    """
    N Candles v3: consecutive same-direction candles entry with StartProtection SL/TP.
    """

    def __init__(self):
        super(n_candles_v3_strategy, self).__init__()
        self._identical = self.Param("IdenticalCandles", 3).SetDisplay("Identical", "Required consecutive candles", "Pattern")
        self._tp_points = self.Param("TakeProfitPoints", 50.0).SetDisplay("TP Points", "Take profit steps", "Risk")
        self._sl_points = self.Param("StopLossPoints", 50.0).SetDisplay("SL Points", "Stop loss steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candles", "General")

        self._seq_dir = 0
        self._seq_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(n_candles_v3_strategy, self).OnReseted()
        self._seq_dir = 0
        self._seq_count = 0

    def OnStarted2(self, time):
        super(n_candles_v3_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        tp = float(self._tp_points.Value)
        sl = float(self._sl_points.Value)
        if tp > 0 or sl > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Absolute) if tp > 0 else Unit(0, UnitTypes.Absolute),
                Unit(sl, UnitTypes.Absolute) if sl > 0 else Unit(0, UnitTypes.Absolute)
            )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        direction = 1 if close > open_p else (-1 if close < open_p else 0)
        if direction == 0:
            self._seq_dir = 0
            self._seq_count = 0
            return
        if self._seq_dir == direction:
            self._seq_count += 1
        else:
            self._seq_dir = direction
            self._seq_count = 1
        if self._seq_count < self._identical.Value:
            return
        if self._seq_dir > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._seq_dir < 0 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return n_candles_v3_strategy()
