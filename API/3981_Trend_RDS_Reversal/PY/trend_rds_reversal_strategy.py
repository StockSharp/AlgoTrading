import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class trend_rds_reversal_strategy(Strategy):
    """Three-bar momentum reversal with EMA filter and StartProtection SL/TP."""
    def __init__(self):
        super(trend_rds_reversal_strategy, self).__init__()
        self._sl = self.Param("StopLoss", 500.0).SetDisplay("Stop Loss", "Stop-loss distance", "Risk")
        self._tp = self.Param("TakeProfit", 500.0).SetDisplay("Take Profit", "Take-profit distance", "Risk")
        self._depth = self.Param("MaxPatternDepth", 10).SetGreaterThanZero().SetDisplay("Pattern Depth", "Max candles for pattern", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trend_rds_reversal_strategy, self).OnReseted()
        self._extremes = []

    def OnStarted(self, time):
        super(trend_rds_reversal_strategy, self).OnStarted(time)
        self._extremes = []

        ema = ExponentialMovingAverage()
        ema.Length = 5

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        tp_val = float(self._tp.Value)
        sl_val = float(self._sl.Value)
        tp = Unit(tp_val, UnitTypes.Absolute) if tp_val > 0 else None
        sl = Unit(sl_val, UnitTypes.Absolute) if sl_val > 0 else None
        self.StartProtection(tp, sl)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        depth = self._depth.Value

        self._extremes.insert(0, (high, low))
        if len(self._extremes) > depth + 2:
            self._extremes.pop()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if len(self._extremes) < 3:
            return

        buy_signal, sell_signal = self._detect_signals()

        if buy_signal:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            if self.Position <= 0:
                self.BuyMarket(self.Volume)
        elif sell_signal:
            if self.Position > 0:
                self.SellMarket(self.Position)
            if self.Position >= 0:
                self.SellMarket(self.Volume)

    def _detect_signals(self):
        depth = min(len(self._extremes) - 2, self._depth.Value)
        if depth <= 0:
            return False, False

        for i in range(depth):
            if i + 2 >= len(self._extremes):
                break
            first = self._extremes[i]
            second = self._extremes[i + 1]
            third = self._extremes[i + 2]

            conflict = (first[0] < second[0] and second[0] < third[0] and
                        first[1] > second[1] and second[1] > third[1])

            # Rising lows -> buy
            if not conflict and first[1] > second[1] and second[1] > third[1]:
                return True, False

            # Rising highs -> sell
            if not conflict and first[0] < second[0] and second[0] < third[0]:
                return False, True

        return False, False

    def CreateClone(self):
        return trend_rds_reversal_strategy()
