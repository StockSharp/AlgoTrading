import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class expert610_breakout_strategy(Strategy):
    """
    Expert610 Breakout: Previous candle high/low breakout with EMA trend filter and ATR stops.
    """

    def __init__(self):
        super(expert610_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Indicators")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(expert610_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(expert610_breakout_strategy, self).OnStarted(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_val)
        atr_val = float(atr_val)

        if self._prev_high == 0.0 or self._prev_low == 0.0 or atr_val <= 0:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            return

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 2.5 or close <= self._entry_price - atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 2.5 or close >= self._entry_price + atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if close > self._prev_high and close > ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif close < self._prev_low and close < ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return expert610_breakout_strategy()
