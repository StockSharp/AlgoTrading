import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class n_candles_v2_strategy(Strategy):
    """
    N Candles v2: trades after N consecutive same-direction candles with manual SL/TP.
    """

    def __init__(self):
        super(n_candles_v2_strategy, self).__init__()
        self._candles_count = self.Param("CandlesCount", 3).SetDisplay("Candles", "Consecutive candles required", "Entry")
        self._tp_pips = self.Param("TakeProfitPips", 50).SetDisplay("TP Pips", "Take profit steps", "Risk")
        self._sl_pips = self.Param("StopLossPips", 50).SetDisplay("SL Pips", "Stop loss steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candles", "General")

        self._streak_len = 0
        self._streak_dir = 0
        self._pos_dir = 0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(n_candles_v2_strategy, self).OnReseted()
        self._streak_len = 0
        self._streak_dir = 0
        self._pos_dir = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(n_candles_v2_strategy, self).OnStarted(time)
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
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        # manage open position
        if self.Position != 0:
            step = 1.0
            tp = self._tp_pips.Value
            sl = self._sl_pips.Value
            if self.Position > 0:
                if tp > 0 and high >= self._entry_price + tp * step:
                    self.SellMarket()
                    self._pos_dir = 0
                    return
                if sl > 0 and low <= self._entry_price - sl * step:
                    self.SellMarket()
                    self._pos_dir = 0
                    return
            elif self.Position < 0:
                if tp > 0 and low <= self._entry_price - tp * step:
                    self.BuyMarket()
                    self._pos_dir = 0
                    return
                if sl > 0 and high >= self._entry_price + sl * step:
                    self.BuyMarket()
                    self._pos_dir = 0
                    return
        open_p = float(candle.OpenPrice)
        direction = 1 if close > open_p else (-1 if close < open_p else 0)
        if direction == 0:
            self._streak_len = 0
            self._streak_dir = 0
            return
        if direction == self._streak_dir:
            self._streak_len += 1
        else:
            self._streak_dir = direction
            self._streak_len = 1
        if self._streak_len < self._candles_count.Value:
            return
        if direction > 0 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._pos_dir = 1
        elif direction < 0 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._pos_dir = -1

    def CreateClone(self):
        return n_candles_v2_strategy()
