import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class exp_amstell_strategy(Strategy):
    """
    Grid-style strategy that scales into positions on ATR-based price movements.
    Enters based on EMA direction, scales in on pullbacks, exits on ATR-based TP/SL.
    """

    def __init__(self):
        super(exp_amstell_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for grid distance", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period for trend", "Indicators")

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_amstell_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0

    def OnStarted2(self, time):
        super(exp_amstell_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_val)
        ema_val = float(ema_val)

        if atr_val <= 0 or self._prev_ema == 0:
            self._prev_ema = ema_val
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0.0
                self._grid_count = 0
            elif close <= self._entry_price - atr_val * 3.0:
                self.SellMarket()
                self._entry_price = 0.0
                self._grid_count = 0
            elif self._grid_count < 3 and close <= self._entry_price - atr_val:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.BuyMarket()
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0.0
                self._grid_count = 0
            elif close >= self._entry_price + atr_val * 3.0:
                self.BuyMarket()
                self._entry_price = 0.0
                self._grid_count = 0
            elif self._grid_count < 3 and close >= self._entry_price + atr_val:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.SellMarket()

        if self.Position == 0:
            if close > ema_val and ema_val > self._prev_ema:
                self._entry_price = close
                self._grid_count = 0
                self.BuyMarket()
            elif close < ema_val and ema_val < self._prev_ema:
                self._entry_price = close
                self._grid_count = 0
                self.SellMarket()

        self._prev_ema = ema_val

    def CreateClone(self):
        return exp_amstell_strategy()
