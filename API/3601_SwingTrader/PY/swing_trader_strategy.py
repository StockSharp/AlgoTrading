import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class swing_trader_strategy(Strategy):
    """Bollinger Band touch swing: buy on lower touch + middle cross up, sell on upper touch + middle cross down."""
    def __init__(self):
        super(swing_trader_strategy, self).__init__()
        self._bb_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BollingerWidth", 2.0).SetGreaterThanZero().SetDisplay("BB Width", "Bollinger Bands deviation", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe for signals", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(swing_trader_strategy, self).OnReseted()
        self._up_touch = False
        self._down_touch = False
        self._prev_close = 0
        self._prev_middle = 0

    def OnStarted(self, time):
        super(swing_trader_strategy, self).OnStarted(time)
        self._up_touch = False
        self._down_touch = False
        self._prev_close = 0
        self._prev_middle = 0

        self._bb = BollingerBands()
        self._bb.Length = self._bb_period.Value
        self._bb.Width = self._bb_width.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._bb, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._bb.IsFormed:
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)
        middle = float(bb_val.MovingAverage)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Track Bollinger touches
        if high > upper:
            self._up_touch = True
            self._down_touch = False
        if low < lower:
            self._down_touch = True
            self._up_touch = False

        if self._prev_close == 0 or self._prev_middle == 0:
            self._prev_close = close
            self._prev_middle = middle
            return

        # Buy: had lower band touch, now price crosses above middle
        buy_signal = self._down_touch and self._prev_close < self._prev_middle and close > middle
        # Sell: had upper band touch, now price crosses below middle
        sell_signal = self._up_touch and self._prev_close > self._prev_middle and close < middle

        if buy_signal:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
            self._down_touch = False
        elif sell_signal:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
            self._up_touch = False

        self._prev_close = close
        self._prev_middle = middle

    def CreateClone(self):
        return swing_trader_strategy()
