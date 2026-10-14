import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_range_breakout_strategy(Strategy):
    """
    ADX Range Breakout strategy.
    Buys when the close reaches the highest close of the previous HighestPeriod candles while ADX is below AdxThreshold, at most
    MaxTradesPerDay times per trading day (UTC). A fixed StopLoss in price units protects the position and it is closed on the last
    candle of the day.
    """

    def __init__(self):
        super(adx_range_breakout_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "ADX period", "Indicators")
        self._highest_period = self.Param("HighestPeriod", 34).SetGreaterThanZero().SetDisplay("Highest Period", "Previous candles whose highest close must be reached", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 17.5).SetDisplay("ADX Threshold", "ADX level the market must stay below", "Indicators")
        self._stop_loss = self.Param("StopLoss", 1000.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 3).SetGreaterThanZero().SetDisplay("Max Trades Per Day", "Maximum entries per trading day", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._current_day = None
        self._trades_today = 0

    def OnReseted(self):
        super(adx_range_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(adx_range_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss.Value), UnitTypes.Absolute), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        period = self._highest_period.Value
        close = candle.ClosePrice
        previous_highest = max(self._closes) if len(self._closes) >= period else None

        self._closes.append(close)
        while len(self._closes) > period:
            self._closes.pop(0)

        day = candle.OpenTime.Date
        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._trades_today = 0

        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        adx = adx_value.MovingAverage

        if previous_highest is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        frame = self.candle_type.Arg if isinstance(self.candle_type.Arg, TimeSpan) else TimeSpan.Zero
        last_of_day = candle.OpenTime.Add(frame).Date > day

        if self.Position > 0:
            if last_of_day:
                self.SellMarket(self.Position)
            return

        if (not last_of_day and self.Position == 0 and self._trades_today < self._max_trades_per_day.Value
                and close >= previous_highest and adx < Decimal(self._adx_threshold.Value)):
            self.BuyMarket(self.Volume)
            self._trades_today += 1

    def CreateClone(self):
        return adx_range_breakout_strategy()
