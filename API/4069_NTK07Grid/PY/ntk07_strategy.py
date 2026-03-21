import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage

class ntk07_strategy(Strategy):
    def __init__(self):
        super(ntk07_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "Period for ATR", "Indicators")
        self._grid_multiplier = self.Param("GridMultiplier", 1.5) \
            .SetDisplay("Grid Multiplier", "ATR multiplier for grid step", "Grid")

        self._reference_price = 0.0
        self._entry_price = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def GridMultiplier(self):
        return self._grid_multiplier.Value

    def OnStarted(self, time):
        super(ntk07_strategy, self).OnStarted(time)

        self._reference_price = 0.0
        self._entry_price = 0.0
        self._initialized = False

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        ema_val = float(ema_value)

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        grid_step = atr_val * float(self.GridMultiplier)

        if not self._initialized:
            self._reference_price = close
            self._initialized = True
            return

        # Position management
        if self.Position > 0:
            if self._entry_price > 0 and close >= self._entry_price + grid_step * 2:
                self.SellMarket()
                self._reference_price = close
            elif self._entry_price > 0 and close <= self._entry_price - grid_step * 1.5:
                self.SellMarket()
                self._reference_price = close
        elif self.Position < 0:
            if self._entry_price > 0 and close <= self._entry_price - grid_step * 2:
                self.BuyMarket()
                self._reference_price = close
            elif self._entry_price > 0 and close >= self._entry_price + grid_step * 1.5:
                self.BuyMarket()
                self._reference_price = close

        # Entry: price moves a full grid step from reference
        if self.Position == 0:
            if close > self._reference_price + grid_step and close > ema_val:
                self._entry_price = close
                self._reference_price = close
                self.BuyMarket()
            elif close < self._reference_price - grid_step and close < ema_val:
                self._entry_price = close
                self._reference_price = close
                self.SellMarket()

    def OnReseted(self):
        super(ntk07_strategy, self).OnReseted()
        self._reference_price = 0.0
        self._entry_price = 0.0
        self._initialized = False

    def CreateClone(self):
        return ntk07_strategy()
