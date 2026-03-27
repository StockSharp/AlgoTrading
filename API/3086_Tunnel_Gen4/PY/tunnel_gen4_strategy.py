import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class tunnel_gen4_strategy(Strategy):
    """Bollinger Band tunnel: buy on cross above lower band, sell on cross below upper."""
    def __init__(self):
        super(tunnel_gen4_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 20).SetGreaterThanZero().SetDisplay("BB Length", "BB period", "Indicator")
        self._bb_width = self.Param("BbWidth", 2.0).SetGreaterThanZero().SetDisplay("BB Width", "BB std devs", "Indicator")
        self._step_pips = self.Param("StepPips", 50.0).SetGreaterThanZero().SetDisplay("Step Pips", "Profit target distance", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tunnel_gen4_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_upper = 0
        self._prev_lower = 0
        self._entry_price = 0

    def OnStarted(self, time):
        super(tunnel_gen4_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._prev_upper = 0
        self._prev_lower = 0
        self._entry_price = 0

        self._bb = BollingerBands()
        self._bb.Length = self._bb_length.Value
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

        upper = None
        lower = None
        for inner in bb_val.InnerValues:
            name = str(inner.Key.Name) if hasattr(inner.Key, 'Name') else str(inner.Key)
            if "Up" in name or "up" in name:
                upper = float(inner.Value) if not inner.Value.IsEmpty else None
            elif "Low" in name or "low" in name or "Down" in name or "down" in name:
                lower = float(inner.Value) if not inner.Value.IsEmpty else None

        if upper is None or lower is None:
            return

        close = float(candle.ClosePrice)

        if self._prev_close == 0:
            self._prev_close = close
            self._prev_upper = upper
            self._prev_lower = lower
            return

        # Buy: price crosses above lower band from below
        if self._prev_close < self._prev_lower and close >= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close

        # Sell: price crosses below upper band from above
        elif self._prev_close > self._prev_upper and close <= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close

        # Exit on profit target
        step = self._step_pips.Value
        if self.Position > 0 and self._entry_price > 0:
            if close >= self._entry_price + step:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0 and self._entry_price > 0:
            if close <= self._entry_price - step:
                self.BuyMarket()
                self._entry_price = 0

        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return tunnel_gen4_strategy()
