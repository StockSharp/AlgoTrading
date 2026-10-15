import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class full_candle_strategy(Strategy):
    """
    Full Candle strategy.
    Goes long on a bullish candle closing above the EMA whose upper shadow is at most ShadowPercent of the candle range,
    and short on a bearish candle closing below the EMA whose lower shadow is at most ShadowPercent of the range. The
    opposite signal reverses the position; percent take-profit and stop-loss manage the trade (0 disables either).
    """

    def __init__(self):
        super(full_candle_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period", "Indicators")
        self._shadow_percent = self.Param("ShadowPercent", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Shadow %", "Maximum breakout-side shadow in percent of the candle range", "Signals")
        self._tp_percent = self.Param("TPPercent", 1.2) \
            .SetNotNegative() \
            .SetDisplay("TP %", "Take-profit percentage, 0 disables", "Risk")
        self._sl_percent = self.Param("SLPercent", 1.8) \
            .SetNotNegative() \
            .SetDisplay("SL %", "Stop-loss percentage, 0 disables", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(full_candle_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema, self._process_candle).Start()

        tp = float(self._tp_percent.Value)
        sl = float(self._sl_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value.GetValue[Decimal](None))
        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        candle_range = high - low

        if candle_range <= 0:
            return

        max_shadow = candle_range * float(self._shadow_percent.Value) / 100.0

        long_signal = close > open_price and close > ema and high - close <= max_shadow
        short_signal = close < open_price and close < ema and close - low <= max_shadow

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return full_candle_strategy()
