import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class well_martin_strategy(Strategy):
    """Bollinger mean reversion, low ADX filter, actual-fill direction and native protection."""

    def __init__(self):
        super(well_martin_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Signal timeframe", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 84).SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Mean and deviation period", "Indicator")
        self._bollinger_width = self.Param("BollingerWidth", 1.8).SetGreaterThanZero() \
            .SetDisplay("Bollinger Width", "Deviation multiplier", "Indicator")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Trend strength smoothing", "Indicator")
        self._adx_level = self.Param("AdxLevel", 25.0).SetNotNegative() \
            .SetDisplay("ADX Level", "Maximum trend strength", "Indicator")
        self._take_profit = self.Param("TakeProfit", 1200.0).SetNotNegative() \
            .SetDisplay("Take Profit", "Price-unit target", "Risk")
        self._stop_loss = self.Param("StopLoss", 1400.0).SetNotNegative() \
            .SetDisplay("Stop Loss", "Price-unit risk", "Risk")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._closes = []
        self._pending = None
        self._entry_order = None
        self._last_executed_entry_side = None

    def OnReseted(self):
        super(well_martin_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(well_martin_strategy, self).OnStarted2(time)
        self._reset_history()
        self.StartProtection(
            takeProfit=Unit(self._take_profit.Value, UnitTypes.Absolute),
            stopLoss=Unit(self._stop_loss.Value, UnitTypes.Absolute))
        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_period.Value)
        self.SubscribeCandles(self.CandleType).BindEx(adx, self.ProcessCandle).Start()

    def OnOwnTradeReceived(self, trade):
        super(well_martin_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order is not None and trade.Order == self._entry_order:
            self._last_executed_entry_side = trade.Order.Side

    def ProcessCandle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return
        length = int(self._bollinger_period.Value)
        self._closes.append(float(candle.ClosePrice))
        if len(self._closes) > length:
            self._closes.pop(0)
        if len(self._closes) < length or not adx_value.IsFormed:
            return
        strength_raw = adx_value.MovingAverage
        if strength_raw is None:
            return
        strength = float(strength_raw)
        mean = sum(self._closes) / length
        variance = sum((price - mean) ** 2 for price in self._closes) / length
        deviation = variance ** 0.5
        lower = mean - float(self._bollinger_width.Value) * deviation
        upper = mean + float(self._bollinger_width.Value) * deviation

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return

        price = float(candle.ClosePrice)
        if self.Position > 0:
            if price >= upper:
                self._pending = self.SellMarket(abs(self.Position))
            return
        if self.Position < 0:
            if price <= lower:
                self._pending = self.BuyMarket(abs(self.Position))
            return
        if strength >= float(self._adx_level.Value):
            return

        if price < lower and self._last_executed_entry_side in (None, Sides.Buy):
            self._entry_order = self._pending = self.BuyMarket(self.Volume)
        elif price > upper and self._last_executed_entry_side in (None, Sides.Sell):
            self._entry_order = self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return well_martin_strategy()
