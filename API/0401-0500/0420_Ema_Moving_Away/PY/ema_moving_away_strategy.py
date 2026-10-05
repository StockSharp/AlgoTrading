import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_moving_away_strategy(Strategy):
    """
    EMA Moving Away strategy (long only).
    Buys when the close is at least MovingAwayPercent below the EMA, optionally requiring a streak of bearish candles
    (BearishStreak) and a minimum candle body (MinBodyPercent). The long closes when price returns to the EMA, and a
    percent stop-loss protects it if the reversion fails.
    """

    def __init__(self):
        super(ema_moving_away_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 55) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period", "Moving Average")
        self._moving_away_percent = self.Param("MovingAwayPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Moving away (%)", "Required distance of the close below the EMA", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._bearish_streak = self.Param("BearishStreak", 0) \
            .SetNotNegative() \
            .SetDisplay("Bearish Streak", "Consecutive bearish candles required, 0 disables", "Filters")
        self._min_body_percent = self.Param("MinBodyPercent", 0.0) \
            .SetNotNegative() \
            .SetDisplay("Min Body %", "Minimum body of the signal candle in percent, 0 disables", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._bearish_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(ema_moving_away_strategy, self).OnReseted()
        self._bearish_count = 0

    def OnStarted2(self, time):
        super(ema_moving_away_strategy, self).OnStarted2(time)

        self._bearish_count = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ema, self._process_candle).Start()

        stop_loss = float(self._stop_loss_percent.Value)
        if stop_loss > 0:
            self.StartProtection(Unit(), Unit(Decimal(stop_loss), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._bearish_count = self._bearish_count + 1 if candle.ClosePrice < candle.OpenPrice else 0

        if not ema_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if self.Position > 0:
            if close >= ema:
                self.SellMarket(self.Position)
            return

        streak = int(self._bearish_streak.Value)
        min_body = float(self._min_body_percent.Value)

        stretched = close <= ema * (1.0 - float(self._moving_away_percent.Value) / 100.0)
        streak_ok = streak <= 0 or self._bearish_count >= streak
        body_ok = min_body <= 0 or (open_price > 0 and abs(open_price - close) / open_price * 100.0 >= min_body)

        if stretched and streak_ok and body_ok:
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ema_moving_away_strategy()
