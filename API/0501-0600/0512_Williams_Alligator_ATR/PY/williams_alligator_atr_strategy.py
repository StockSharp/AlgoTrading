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


class williams_alligator_atr_strategy(Strategy):
    """Long-only shifted median-price Alligator crosses with an entry-frozen native ATR stop."""

    def __init__(self):
        super(williams_alligator_atr_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("CandleType", "Finished signal candle type.", "Parameters")
        self._jaw_length = self.Param("JawLength", 13).SetRange(1, 2147483647) \
            .SetDisplay("JawLength", "Jaw smoothed median-price window.", "Parameters")
        self._teeth_length = self.Param("TeethLength", 8).SetRange(1, 2147483647) \
            .SetDisplay("TeethLength", "Teeth smoothed median-price window.", "Parameters")
        self._lips_length = self.Param("LipsLength", 5).SetRange(1, 2147483647) \
            .SetDisplay("LipsLength", "Lips smoothed median-price window.", "Parameters")
        self._jaw_shift = self.Param("JawShift", 8).SetRange(1, 2147483647) \
            .SetDisplay("JawShift", "Completed bars delaying the Jaw line.", "Parameters")
        self._teeth_shift = self.Param("TeethShift", 5).SetRange(1, 2147483647) \
            .SetDisplay("TeethShift", "Completed bars delaying the Teeth line.", "Parameters")
        self._lips_shift = self.Param("LipsShift", 3).SetRange(1, 2147483647) \
            .SetDisplay("LipsShift", "Completed bars delaying the Lips line.", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetRange(1, 2147483647) \
            .SetDisplay("AtrPeriod", "Wilder true-range window.", "Parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", Decimal(2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("AtrMultiplier", "Entry-frozen ATR stop multiplier.", "Parameters")
        self._clear_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def JawLength(self):
        return self._jaw_length.Value

    @JawLength.setter
    def JawLength(self, value):
        self._jaw_length.Value = value

    @property
    def TeethLength(self):
        return self._teeth_length.Value

    @TeethLength.setter
    def TeethLength(self, value):
        self._teeth_length.Value = value

    @property
    def LipsLength(self):
        return self._lips_length.Value

    @LipsLength.setter
    def LipsLength(self, value):
        self._lips_length.Value = value

    @property
    def JawShift(self):
        return self._jaw_shift.Value

    @JawShift.setter
    def JawShift(self, value):
        self._jaw_shift.Value = value

    @property
    def TeethShift(self):
        return self._teeth_shift.Value

    @TeethShift.setter
    def TeethShift(self, value):
        self._teeth_shift.Value = value

    @property
    def LipsShift(self):
        return self._lips_shift.Value

    @LipsShift.setter
    def LipsShift(self, value):
        self._lips_shift.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    def _clear_state(self):
        self._pending = None
        self._jaw = None
        self._teeth = None
        self._lips = None
        self._atr = None
        self._previous_spread = None

    def OnReseted(self):
        super(williams_alligator_atr_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):

        super(williams_alligator_atr_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        self._jaw = AlligatorLine()
        self._jaw.Length = self.JawLength
        self._jaw.Shift = self.JawShift
        self._teeth = AlligatorLine()
        self._teeth.Length = self.TeethLength
        self._teeth.Shift = self.TeethShift
        self._lips = AlligatorLine()
        self._lips.Length = self.LipsLength
        self._lips.Shift = self.LipsShift
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

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

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        jaw = self._process(self._jaw, candle)
        teeth = self._process(self._teeth, candle)
        lips = self._process(self._lips, candle)
        atr = self._process(self._atr, candle)
        if not self._jaw.IsFormed or not self._teeth.IsFormed or not self._lips.IsFormed or not self._atr.IsFormed or jaw is None or teeth is None or lips is None or atr is None:
            return
        spread = lips - jaw
        previous = self._previous_spread
        self._previous_spread = spread
        if previous is None or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if previous >= 0 and spread < 0 and self.Position > 0:
            self._submit(Sides.Sell, self.Position, "0512CrossExit")
        elif previous <= 0 and spread > 0 and self.Position == 0 and atr > 0:
            self.StartProtection(Unit(), Unit(atr * self.AtrMultiplier, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
            self._submit(Sides.Buy, self.Volume, "0512Entry")

    def CreateClone(self):
        return williams_alligator_atr_strategy()
