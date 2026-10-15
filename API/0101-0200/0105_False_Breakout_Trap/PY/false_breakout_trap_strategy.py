import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class false_breakout_trap_strategy(Strategy):
    """
    False Breakout Trap strategy.
    Detects when price breaks a recent high/low range then reverses back.
    Trades against the failed breakout direction.
    Exits on the opposite failed breakout or a percent stop beyond the failed breakout level.
    """

    def __init__(self):
        super(false_breakout_trap_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetRange(5, 50).SetDisplay("Lookback", "Period for high/low range", "Range")
        self._stop_loss = self.Param("StopLoss", 2.0).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop distance in percent beyond the failed breakout level", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._highs = []
        self._lows = []
        self._stop_price = None
        self._exit_order = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(false_breakout_trap_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._stop_price = None
        self._exit_order = None

    def OnStarted2(self, time):
        super(false_breakout_trap_strategy, self).OnStarted2(time)

        self._highs = []
        self._lows = []
        self._stop_price = None
        self._exit_order = None

        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._process_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _is_exit_pending(self):
        return self._exit_order is not None and self._exit_order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_quote(self, message):
        if self.Position > 0:
            field = Level1Fields.BestBidPrice
        elif self.Position < 0:
            field = Level1Fields.BestAskPrice
        else:
            return

        if not message.Changes.ContainsKey(field):
            return

        price = message.Changes[field]
        if price is not None and price > Decimal(0):
            self._try_stop_out(price)

    # Long positions are tested against a bid or bar low, short positions against an ask or bar high.
    def _try_stop_out(self, price):
        if self._stop_price is None or self._is_exit_pending():
            return False

        if (self.Position > 0 and price <= self._stop_price) or (self.Position < 0 and price >= self._stop_price):
            self._exit_position()
            return True

        return False

    def _exit_position(self):
        if self.Position > 0:
            self._exit_order = self.SellMarket(self.Position)
        else:
            self._exit_order = self.BuyMarket(-self.Position)
        self._stop_price = None

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        lookback = self._lookback_period.Value

        # Maintain rolling high/low window
        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)
        if len(self._highs) > lookback + 1:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < lookback + 1:
            return

        if self._try_stop_out(candle.LowPrice if self.Position > 0 else candle.HighPrice):
            return

        # Find highest high and lowest low of the previous N bars (excluding current)
        range_high = max(self._highs[:-1])
        range_low = min(self._lows[:-1])

        # False upside breakout: candle broke above range high but closed below it
        false_break_up = candle.HighPrice > range_high and candle.ClosePrice < range_high
        # False downside breakout: candle broke below range low but closed above it
        false_break_down = candle.LowPrice < range_low and candle.ClosePrice > range_low

        stop_fraction = Decimal(self._stop_loss.Value) / Decimal(100)

        if (self.Position > 0 and false_break_up) or (self.Position < 0 and false_break_down):
            if not self._is_exit_pending():
                self._exit_position()
        elif self.Position == 0 and false_break_down:
            self._stop_price = range_low * (Decimal(1) - stop_fraction)
            self.BuyMarket()
        elif self.Position == 0 and false_break_up:
            self._stop_price = range_high * (Decimal(1) + stop_fraction)
            self.SellMarket()

    def CreateClone(self):
        return false_breakout_trap_strategy()
