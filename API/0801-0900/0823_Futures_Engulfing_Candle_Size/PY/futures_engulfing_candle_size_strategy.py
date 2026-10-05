import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class futures_engulfing_candle_size_strategy(Strategy):
    """
    Futures Engulfing Candle Size strategy.
    Inside the StartHour:StartMinute..EndHour:EndMinute window (candle open time, UTC) the first candle whose high-low range reaches
    CandleSizeThresholdTicks price steps opens one trade for the day in the direction of its body. The trade exits through a
    take profit of TakeProfitTicks and a stop loss of StopLossTicks price steps.
    """

    def __init__(self):
        super(futures_engulfing_candle_size_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._candle_size_threshold_ticks = self.Param("CandleSizeThresholdTicks", 25).SetGreaterThanZero().SetDisplay("Candle Size Ticks", "Minimum candle range in price steps", "Signal")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 50).SetNotNegative().SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk")
        self._stop_loss_ticks = self.Param("StopLossTicks", 40).SetNotNegative().SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk")
        self._start_hour = self.Param("StartHour", 7).SetRange(0, 23).SetDisplay("Start Hour", "Session start hour (UTC)", "Session")
        self._start_minute = self.Param("StartMinute", 0).SetRange(0, 59).SetDisplay("Start Minute", "Session start minute", "Session")
        self._end_hour = self.Param("EndHour", 9).SetRange(0, 23).SetDisplay("End Hour", "Session end hour (UTC)", "Session")
        self._end_minute = self.Param("EndMinute", 15).SetRange(0, 59).SetDisplay("End Minute", "Session end minute", "Session")
        self._last_trade_day = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(futures_engulfing_candle_size_strategy, self).OnReseted()
        self._last_trade_day = None

    def _price_step(self):
        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else None
        step = float(step) if step is not None else 1.0
        return step if step > 0 else 1.0

    def OnStarted2(self, time):
        super(futures_engulfing_candle_size_strategy, self).OnStarted2(time)

        self._last_trade_day = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        step = self._price_step()
        self.StartProtection(
            Unit(Decimal(self._take_profit_ticks.Value * step), UnitTypes.Absolute),
            Unit(Decimal(self._stop_loss_ticks.Value * step), UnitTypes.Absolute),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        time = candle.OpenTime
        day = time.Date

        if self._last_trade_day == day or self.Position != 0:
            return

        minutes = time.Hour * 60 + time.Minute
        start = self._start_hour.Value * 60 + self._start_minute.Value
        end = self._end_hour.Value * 60 + self._end_minute.Value
        if minutes < start or minutes > end:
            return

        if (float(candle.HighPrice) - float(candle.LowPrice)) / self._price_step() < self._candle_size_threshold_ticks.Value:
            return

        if candle.ClosePrice > candle.OpenPrice:
            self.BuyMarket()
            self._last_trade_day = day
        elif candle.ClosePrice < candle.OpenPrice:
            self.SellMarket()
            self._last_trade_day = day

    def CreateClone(self):
        return futures_engulfing_candle_size_strategy()
