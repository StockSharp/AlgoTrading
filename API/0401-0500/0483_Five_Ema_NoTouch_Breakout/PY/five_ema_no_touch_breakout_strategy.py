import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, Math, TimeSpan, InvalidOperationException
from System.Globalization import CultureInfo, DateTimeStyles
from System.IO import File
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange, AlligatorLine, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import Level1Fields, ITickTradeMessage, CandleStates, DataType, OrderStates, OrderTypes, Sides, Unit, UnitTypes


class five_ema_no_touch_breakout_strategy(Strategy):
    """Finished no-touch EMA setups with native protection fixed from actual entry risk."""

    def __init__(self):
        super(five_ema_no_touch_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Finished signal candle type.", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 5).SetRange(1, 2147483647) \
            .SetDisplay("EmaPeriod", "EMA length.", "Parameters")
        self._reward_risk = self.Param("RewardRisk", Decimal(3)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("RewardRisk", "Take-profit distance divided by initial risk.", "Parameters")
        self._clear_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def RewardRisk(self):
        return self._reward_risk.Value

    @RewardRisk.setter
    def RewardRisk(self, value):
        self._reward_risk.Value = value

    def _clear_state(self):
        self._pending = None
        self._ema = None
        self._setup = None
        self._setup_stop = Decimal(0)
        self._is_protection_armed = False

    def OnReseted(self):
        super(five_ema_no_touch_breakout_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):

        super(five_ema_no_touch_breakout_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self.SubscribeCandles(self.CandleType).Bind(self._ema, self._process_candle).Start()

    def _is_pending(self):
        return self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)

    def _submit(self, side, quantity, comment):
        self._pending = self.CreateOrder(side, Decimal(0), quantity)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = comment
        self.RegisterOrder(self._pending)

    @staticmethod
    def _process(indicator, candle):
        value = CandleIndicatorValue(indicator, candle)
        value.IsFinal = True
        result = indicator.Process(value)
        return None if result.IsEmpty else result.GetValue[Decimal](None)

    def _process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished or not self._ema.IsFormed or not self.IsFormedAndOnlineAndAllowTrading() or self._is_pending():
            return
        if self.Position != 0:
            self._setup = None
            return
        if self._setup is not None:
            high, low, side = self._setup
            if (side == Sides.Buy and candle.HighPrice > high and candle.ClosePrice > low) or (side == Sides.Sell and candle.LowPrice < low and candle.ClosePrice < high):
                self._setup_stop = low if side == Sides.Buy else high
                self._is_protection_armed = False
                self._setup = None
                self._submit(side, self.Volume, "0483Entry")
                return
        if candle.HighPrice < ema:
            self._setup = (candle.HighPrice, candle.LowPrice, Sides.Buy)
        elif candle.LowPrice > ema:
            self._setup = (candle.HighPrice, candle.LowPrice, Sides.Sell)

    def OnOwnTradeReceived(self, trade):
        super(five_ema_no_touch_breakout_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Comment != "0483Entry" or self._is_protection_armed:
            return
        self._is_protection_armed = True
        risk = ITickTradeMessage.Price.__get__(trade.Trade) - self._setup_stop if trade.Order.Side == Sides.Buy else self._setup_stop - ITickTradeMessage.Price.__get__(trade.Trade)
        if risk <= 0:
            self._submit(Sides.Sell if trade.Order.Side == Sides.Buy else Sides.Buy, abs(self.Position), "0483InvalidFillExit")
            return
        self.StartProtection(Unit(risk * self.RewardRisk, UnitTypes.Absolute), Unit(risk, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)

    def CreateClone(self):
        return five_ema_no_touch_breakout_strategy()
