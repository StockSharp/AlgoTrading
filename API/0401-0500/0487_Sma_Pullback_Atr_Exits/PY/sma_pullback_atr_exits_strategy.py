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


class sma_pullback_atr_exits_strategy(Strategy):
    """SMA pullbacks with entry-frozen ATR distances and native quote exits."""

    def __init__(self):
        super(sma_pullback_atr_exits_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("CandleType", "Finished signal candle type.", "Parameters")
        self._fast_sma_length = self.Param("FastSmaLength", 8).SetRange(1, 2147483647) \
            .SetDisplay("FastSmaLength", "Fast close SMA length.", "Parameters")
        self._slow_sma_length = self.Param("SlowSmaLength", 30).SetRange(2, 2147483647) \
            .SetDisplay("SlowSmaLength", "Slow close SMA length.", "Parameters")
        self._atr_length = self.Param("AtrLength", 14).SetRange(1, 2147483647) \
            .SetDisplay("AtrLength", "Wilder true-range window.", "Parameters")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", Decimal(1.2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("AtrMultiplierSl", "Frozen ATR stop-loss multiplier.", "Parameters")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", Decimal(2)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("AtrMultiplierTp", "Frozen ATR take-profit multiplier.", "Parameters")
        self._clear_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastSmaLength(self):
        return self._fast_sma_length.Value

    @FastSmaLength.setter
    def FastSmaLength(self, value):
        self._fast_sma_length.Value = value

    @property
    def SlowSmaLength(self):
        return self._slow_sma_length.Value

    @SlowSmaLength.setter
    def SlowSmaLength(self, value):
        self._slow_sma_length.Value = value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_length.Value = value

    @property
    def AtrMultiplierSl(self):
        return self._atr_multiplier_sl.Value

    @AtrMultiplierSl.setter
    def AtrMultiplierSl(self, value):
        self._atr_multiplier_sl.Value = value

    @property
    def AtrMultiplierTp(self):
        return self._atr_multiplier_tp.Value

    @AtrMultiplierTp.setter
    def AtrMultiplierTp(self, value):
        self._atr_multiplier_tp.Value = value

    def _clear_state(self):
        self._pending = None
        self._fast = None
        self._slow = None
        self._atr = None

    def OnReseted(self):
        super(sma_pullback_atr_exits_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.FastSmaLength >= self.SlowSmaLength:
            raise InvalidOperationException("FastSmaLength must be smaller than SlowSmaLength.")
        super(sma_pullback_atr_exits_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        self._fast = SimpleMovingAverage()
        self._fast.Length = self.FastSmaLength
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.SlowSmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength
        self.SubscribeCandles(self.CandleType).Bind(self._fast, self._slow, self._atr, self._process_candle).Start()

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

    def _process_candle(self, candle, fast, slow, atr):
        if candle.State != CandleStates.Finished or not self._fast.IsFormed or not self._slow.IsFormed or not self._atr.IsFormed or atr <= 0 or self.Position != 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        side = Sides.Buy if candle.ClosePrice < fast and fast > slow else Sides.Sell if candle.ClosePrice > fast and fast < slow else None
        if side is None:
            return
        self.StartProtection(Unit(atr * self.AtrMultiplierTp, UnitTypes.Absolute), Unit(atr * self.AtrMultiplierSl, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
        self._submit(side, self.Volume, "0487Entry")

    def CreateClone(self):
        return sma_pullback_atr_exits_strategy()
