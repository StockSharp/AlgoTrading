import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage

class support_resistance_breakout_strategy(Strategy):
    def __init__(self):
        super(support_resistance_breakout_strategy, self).__init__()

        self._range_length = self.Param("RangeLength", 55) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "Length of the EMA trend filter", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss in absolute points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0) \
            .SetDisplay("Take Profit", "Take profit in absolute points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self.Volume = 1

        self._highs = []
        self._lows = []
        self._support = 0.0
        self._resistance = 0.0
        self._entry_price = None

    @property
    def RangeLength(self):
        return self._range_length.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(support_resistance_breakout_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.ProcessCandle).Start()

        tp = float(self.TakeProfitPoints)
        sl = float(self.StopLossPoints)
        tp_unit = Unit(tp, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(sl, UnitTypes.Absolute) if sl > 0 else None
        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_value = float(ema_value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        self._highs.append(high)
        self._lows.append(low)
        rl = self.RangeLength
        if len(self._highs) > rl:
            self._highs.pop(0)
        if len(self._lows) > rl:
            self._lows.pop(0)

        if len(self._highs) < rl:
            return

        max_high = max(self._highs[:-1])
        min_low = min(self._lows[:-1])
        self._resistance = max_high
        self._support = min_low

        is_bullish = close > ema_value
        is_bearish = close < ema_value

        if self.Position > 0 and self._entry_price is not None:
            if close - self._entry_price > 0 and close < self._support:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0 and self._entry_price is not None:
            if self._entry_price - close > 0 and close > self._resistance:
                self.BuyMarket(abs(self.Position))
                return

        if is_bullish and self.Position <= 0 and close > self._resistance and self._resistance > 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket(self.Volume)
        elif is_bearish and self.Position >= 0 and close < self._support and self._support > 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            self.SellMarket(self.Volume)

    def OnOwnTradeReceived(self, trade):
        super(support_resistance_breakout_strategy, self).OnOwnTradeReceived(trade)
        if self.Position != 0 and self._entry_price is None:
            self._entry_price = float(trade.Trade.Price)
        if self.Position == 0:
            self._entry_price = None

    def OnReseted(self):
        super(support_resistance_breakout_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._support = 0.0
        self._resistance = 0.0
        self._entry_price = None

    def CreateClone(self):
        return support_resistance_breakout_strategy()
