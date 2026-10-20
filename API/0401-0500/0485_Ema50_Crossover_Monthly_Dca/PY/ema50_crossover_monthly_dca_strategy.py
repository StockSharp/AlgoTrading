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
from StockSharp.Messages import ITickTradeMessage, CandleStates, DataType, OrderStates, OrderTypes, Sides, Unit, UnitTypes


class ema50_crossover_monthly_dca_strategy(Strategy):
    """Monthly cash contributions invested above a formed weekly EMA, with complete long exits below it."""

    def __init__(self):
        super(ema50_crossover_monthly_dca_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10080))) \
            .SetDisplay("CandleType", "Finished signal candle type.", "Parameters")
        self._ema_length = self.Param("EmaLength", 50).SetRange(1, 2147483647) \
            .SetDisplay("EmaLength", "Weekly close EMA length.", "Parameters")
        self._dca_amount = self.Param("DcaAmount", Decimal(100000)).SetRange(Decimal(0.000001), Decimal.MaxValue) \
            .SetDisplay("DcaAmount", "Cash contribution per observed calendar month.", "Parameters")
        self._start_date = self.Param("StartDate", DateTime(1980, 1, 1)) \
            .SetDisplay("StartDate", "Earliest eligible historical candle close.", "Parameters")
        self._clear_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @EmaLength.setter
    def EmaLength(self, value):
        self._ema_length.Value = value

    @property
    def DcaAmount(self):
        return self._dca_amount.Value

    @DcaAmount.setter
    def DcaAmount(self, value):
        self._dca_amount.Value = value

    @property
    def StartDate(self):
        return self._start_date.Value

    @StartDate.setter
    def StartDate(self, value):
        self._start_date.Value = value

    def _clear_state(self):
        self._pending = None
        self._ema = None
        self._cash = Decimal(0)
        self._last_month = 0

    def OnReseted(self):
        super(ema50_crossover_monthly_dca_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.Security.VolumeStep is None or self.Security.MinVolume is None or self.Security.MaxVolume is None or self.Security.VolumeStep <= 0 or self.Security.MinVolume <= 0 or self.Security.MaxVolume <= 0:
            raise InvalidOperationException("Execution security needs native volume constraints.")
        super(ema50_crossover_monthly_dca_strategy, self).OnStarted2(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
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
        if candle.State != CandleStates.Finished or candle.CloseTime < self.StartDate or candle.ClosePrice <= 0:
            return
        month = candle.CloseTime.Year * 12 + candle.CloseTime.Month
        if month > self._last_month:
            self._cash += self.DcaAmount * Decimal(1 if self._last_month == 0 else month - self._last_month)
            self._last_month = month
        if not self._ema.IsFormed or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if candle.ClosePrice < ema and self.Position > 0:
            self._submit(Sides.Sell, self.Position, "0485Exit")
        elif candle.ClosePrice > ema:
            quantity = Math.Floor(min(self._cash / candle.ClosePrice, self.Security.MaxVolume) / self.Security.VolumeStep) * self.Security.VolumeStep
            if quantity >= self.Security.MinVolume:
                self._submit(Sides.Buy, quantity, "0485Dca")

    def OnOwnTradeReceived(self, trade):
        super(ema50_crossover_monthly_dca_strategy, self).OnOwnTradeReceived(trade)
        self._cash += Decimal(-1 if trade.Order.Side == Sides.Buy else 1) * ITickTradeMessage.Price.__get__(trade.Trade) * ITickTradeMessage.Volume.__get__(trade.Trade)

    def CreateClone(self):
        return ema50_crossover_monthly_dca_strategy()
