import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class shooting_star_strategy(Strategy):
    """
    Short-only shooting star after three rising closes, optionally confirmed next bar.
    Protects above the pattern high with executable asks and finished-bar fallback.
    """

    def __init__(self):
        super(shooting_star_strategy, self).__init__()
        self._shadow_to_body_ratio = self.Param("ShadowToBodyRatio", 2.0).SetGreaterThanZero().SetDisplay("Shadow/body ratio", "Minimum upper-shadow to real-body ratio", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe for shooting-star pattern", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetRange(0.0, 99.0).SetDisplay("Stop above high (%)", "Percent buffer above the star high; zero places it at the high.", "Protection")
        self._confirmation_required = self.Param("ConfirmationRequired", True).SetDisplay("Confirmation required", "Wait for the next candle to close below the star close.", "Pattern")
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _clear_state(self):
        self._prior_closes = []
        self._candidate = None
        self._pattern_stop = None
        self._entry_order = None
        self._exit_order = None

    def OnReseted(self):
        super(shooting_star_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(shooting_star_strategy, self).OnStarted2(time)
        self._clear_state()
        asks = Subscription(DataType.Level1, self.Security)
        asks.MarketData.BuildField = Level1Fields.BestAskPrice
        self.SubscribeLevel1(asks).Bind(self._process_ask).Start()
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _is_pending(self, order):
        return order is not None and order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_ask(self, message):
        if message.Changes.ContainsKey(Level1Fields.BestAskPrice):
            ask = message.Changes[Level1Fields.BestAskPrice]
            if ask is not None and ask > Decimal(0):
                self._check_stop(ask)

    def _check_stop(self, executable_ask):
        if self.Position < 0 and self._pattern_stop is not None and executable_ask >= self._pattern_stop and not self._is_pending(self._exit_order):
            self._exit_order = self.BuyMarket(Math.Abs(self.Position))
            self._candidate = None

    def _enter_short(self, pattern_high):
        self._pattern_stop = pattern_high * (Decimal(1) + Decimal(self._stop_loss_percent.Value) / Decimal(100))
        self._entry_order = self.SellMarket(self.Volume)
        self._candidate = None

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        advance = len(self._prior_closes) == 3
        if advance:
            advance = self._prior_closes[0] < self._prior_closes[1] < self._prior_closes[2]
            self._prior_closes.pop(0)
        self._prior_closes.append(candle.ClosePrice)
        if self.Position < 0:
            self._check_stop(candle.HighPrice)
            return
        if self._is_pending(self._entry_order) or self._is_pending(self._exit_order):
            return
        if self._candidate is not None:
            star_high, star_close = self._candidate
            self._candidate = None
            if candle.ClosePrice < star_close and self.IsFormedAndOnlineAndAllowTrading():
                self._enter_short(star_high)
                return
        body = Math.Abs(candle.ClosePrice - candle.OpenPrice)
        upper = candle.HighPrice - max(candle.OpenPrice, candle.ClosePrice)
        lower = min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice
        if not advance or body <= Decimal(0) or upper < body * Decimal(self._shadow_to_body_ratio.Value) or lower > body * Decimal(0.5) or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._confirmation_required.Value:
            self._candidate = (candle.HighPrice, candle.ClosePrice)
        else:
            self._enter_short(candle.HighPrice)

    def CreateClone(self):
        return shooting_star_strategy()
