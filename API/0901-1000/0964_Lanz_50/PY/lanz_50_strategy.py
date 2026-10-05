import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class lanz_50_strategy(Strategy):
    """
    LANZ Strategy 5.0.
    Three consecutive bullish candles closing above the EmaPeriod EMA go long; with EnableSell, three bearish candles below it go short,
    reversing an opposite position. Entries are allowed only between StartHour and EndHour (UTC, the window may cross midnight), at most
    MaxTrades per day and at least MinDistancePips price steps away from the previous entry. A fixed stop and target in price steps protect
    each position, and any open position is closed once the window ends.
    """

    def __init__(self):
        super(lanz_50_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 200).SetGreaterThanZero().SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._max_trades = self.Param("MaxTrades", 99).SetGreaterThanZero().SetDisplay("Max Trades", "Maximum entries per day", "Risk")
        self._min_distance_pips = self.Param("MinDistancePips", 25.0).SetNotNegative().SetDisplay("Min Distance", "Minimum distance from the previous entry in price steps", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 40.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss in price steps", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 120.0).SetNotNegative().SetDisplay("Take Profit", "Take profit in price steps", "Risk")
        self._start_hour = self.Param("StartHour", 19).SetRange(0, 23).SetDisplay("Start Hour", "Hour (UTC) the trading window opens", "Time")
        self._end_hour = self.Param("EndHour", 15).SetRange(0, 23).SetDisplay("End Hour", "Hour (UTC) the trading window closes and positions are closed", "Time")
        self._enable_buy = self.Param("EnableBuy", True).SetDisplay("Enable Buy", "Allow long entries", "Mode")
        self._enable_sell = self.Param("EnableSell", False).SetDisplay("Enable Sell", "Allow short entries", "Mode")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._last_entry_price = None
        self._daily_trades = 0
        self._current_day = None
        self._bullish_count = 0
        self._bearish_count = 0

    def OnReseted(self):
        super(lanz_50_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(lanz_50_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp = float(self._take_profit_pips.Value)
        sl = float(self._stop_loss_pips.Value)
        self.StartProtection(
            Unit(Decimal(tp * step), UnitTypes.Absolute) if tp > 0 else Unit(),
            Unit(Decimal(sl * step), UnitTypes.Absolute) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        ema = float(ema_value)

        self._bullish_count = self._bullish_count + 1 if close > open_p else 0
        self._bearish_count = self._bearish_count + 1 if close < open_p else 0

        time = candle.OpenTime
        if self._current_day is None or time.Date != self._current_day:
            self._current_day = time.Date
            self._daily_trades = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hour = time.Hour
        start = self._start_hour.Value
        end = self._end_hour.Value
        if start <= end:
            in_window = start <= hour < end
        else:
            in_window = hour >= start or hour < end

        if not in_window:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        if self._daily_trades >= self._max_trades.Value:
            return

        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 1.0

        if self._last_entry_price is not None and abs(close - self._last_entry_price) < float(self._min_distance_pips.Value) * step:
            return

        if self._enable_buy.Value and self._bullish_count >= 3 and close > ema and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._daily_trades += 1
            self._last_entry_price = close
        elif self._enable_sell.Value and self._bearish_count >= 3 and close < ema and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._daily_trades += 1
            self._last_entry_price = close

    def CreateClone(self):
        return lanz_50_strategy()
