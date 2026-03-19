import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class locker_hedging_grid_strategy(Strategy):
    """
    Grid strategy using ATR for grid spacing.
    Opens positions at grid steps, exits at TP/SL distances.
    """

    def __init__(self):
        super(locker_hedging_grid_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "Period for ATR", "Indicators")
        self._grid_multiplier = self.Param("GridMultiplier", 1.5) \
            .SetDisplay("Grid Multiplier", "ATR multiplier for grid spacing", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")

        self._grid_level = 0.0
        self._entry_price = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(locker_hedging_grid_strategy, self).OnReseted()
        self._grid_level = 0.0
        self._entry_price = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(locker_hedging_grid_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr = float(atr_val)
        if atr <= 0:
            return

        close = float(candle.ClosePrice)
        grid_step = atr * self._grid_multiplier.Value

        if not self._initialized:
            self._grid_level = close
            self._initialized = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position == 0:
            if close >= self._grid_level + grid_step:
                self._entry_price = close
                self._grid_level = close
                self.BuyMarket()
            elif close <= self._grid_level - grid_step:
                self._entry_price = close
                self._grid_level = close
                self.SellMarket()
        elif self.Position > 0:
            if close >= self._entry_price + grid_step:
                self.SellMarket()
                self._grid_level = close
            elif close <= self._entry_price - grid_step * 2:
                self.SellMarket()
                self._grid_level = close
        elif self.Position < 0:
            if close <= self._entry_price - grid_step:
                self.BuyMarket()
                self._grid_level = close
            elif close >= self._entry_price + grid_step * 2:
                self.BuyMarket()
                self._grid_level = close

    def CreateClone(self):
        return locker_hedging_grid_strategy()
