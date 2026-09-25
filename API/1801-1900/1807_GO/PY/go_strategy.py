import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class go_strategy(Strategy):
    def __init__(self):
        super(go_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 14).SetGreaterThanZero()
        self._open_level = self.Param("OpenLevel", 0.0).SetNotNegative()
        self._close_level_diff = self.Param("CloseLevelDiff", 0.0).SetNotNegative()
        self._show_go = self.Param("ShowGo", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._open_ema = None
        self._high_ema = None
        self._low_ema = None
        self._close_ema = None
        self._samples = 0

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(go_strategy, self).OnReseted()
        self._open_ema = None
        self._high_ema = None
        self._low_ema = None
        self._close_ema = None
        self._samples = 0

    def OnStarted2(self, time):
        super(go_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        period = int(self._ma_period.Value)
        self._samples += 1
        self._open_ema = self._ema(self._open_ema, float(candle.OpenPrice), period)
        self._high_ema = self._ema(self._high_ema, float(candle.HighPrice), period)
        self._low_ema = self._ema(self._low_ema, float(candle.LowPrice), period)
        self._close_ema = self._ema(self._close_ema, float(candle.ClosePrice), period)

        if self._samples < period:
            return

        go = self.calculate_go(
            self._open_ema, self._high_ema, self._low_ema, self._close_ema, float(candle.TotalVolume))

        if bool(self._show_go.Value):
            self.LogInfo("GO={0}", go)

        open_level = float(self._open_level.Value)
        close_level = open_level - float(self._close_level_diff.Value)

        if self.Position > 0:
            if go < close_level:
                self.SellMarket(Math.Abs(self.Position))
            return

        if self.Position < 0:
            if go > -close_level:
                self.BuyMarket(Math.Abs(self.Position))
            return

        if go > open_level:
            self.BuyMarket()
        elif go < -open_level:
            self.SellMarket()

    @staticmethod
    def calculate_go(open_ema, high_ema, low_ema, close_ema, volume):
        return ((close_ema - open_ema) +
                (high_ema - open_ema) +
                (low_ema - open_ema) +
                (close_ema - low_ema) +
                (close_ema - high_ema)) * volume

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = 2.0 / (period + 1.0)
        return previous + alpha * (value - previous)

    def CreateClone(self):
        return go_strategy()
