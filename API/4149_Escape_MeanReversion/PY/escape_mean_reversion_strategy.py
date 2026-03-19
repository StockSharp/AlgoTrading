import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class escape_mean_reversion_strategy(Strategy):
    """
    Escape Mean Reversion: SMA crossover with ATR stops.
    Buys when price crosses below SMA, sells when price crosses above SMA.
    Exits at ATR-based TP/SL levels.
    """

    def __init__(self):
        super(escape_mean_reversion_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._sma_length = self.Param("SmaLength", 5) \
            .SetDisplay("SMA Length", "SMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")

        self._prev_close = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(escape_mean_reversion_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(escape_mean_reversion_strategy, self).OnStarted(time)

        self._prev_close = 0.0
        self._entry_price = 0.0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_val)
        atr_val = float(atr_val)

        if self._prev_close == 0.0 or atr_val <= 0:
            self._prev_close = close
            return

        if self.Position > 0:
            if (close >= self._entry_price + atr_val * 2.0 or
                    close <= self._entry_price - atr_val * 1.5 or
                    close > sma_val):
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (close <= self._entry_price - atr_val * 2.0 or
                    close >= self._entry_price + atr_val * 1.5 or
                    close < sma_val):
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close < sma_val and self._prev_close >= sma_val:
                self._entry_price = close
                self.BuyMarket()
            elif close > sma_val and self._prev_close <= sma_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_close = close

    def CreateClone(self):
        return escape_mean_reversion_strategy()
