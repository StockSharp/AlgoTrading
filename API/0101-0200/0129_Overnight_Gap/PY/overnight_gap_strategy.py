import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class overnight_gap_strategy(Strategy):
    """
    Overnight Gap strategy.
    The market trades around the clock, so the session is the UTC day.
    The gap is the distance between the previous UTC day's last close and the new day's first open. A gap of at least MinGapPercent
    is faded at the close of the day's first candle; the stop lies StopLossPercent beyond the first candle's extreme in the gap's direction,
    and the position closes at the day's last candle.
    """

    def __init__(self):
        super(overnight_gap_strategy, self).__init__()
        self._min_gap_percent = self.Param("MinGapPercent", 0.01).SetGreaterThanZero().SetDisplay("Min Gap %", "Smallest gap to fade, in percent", "Session")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the first candle's extreme, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._prev_close = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(overnight_gap_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(overnight_gap_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        first_of_day = self._day is None or self._day != day
        if first_of_day:
            self._day = day

        frame = self.candle_type.Arg
        last_of_day = (candle.OpenTime + frame).Date != day

        prev_close = self._prev_close
        self._prev_close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        if self.Position != 0:
            stopped = close <= self._stop_price if self.Position > 0 else close >= self._stop_price
            if last_of_day or stopped:
                self._close_position()
            return

        if not first_of_day or prev_close is None or prev_close <= 0:
            return

        gap = (candle.OpenPrice - prev_close) / prev_close * Decimal(100)
        min_gap = Decimal(self._min_gap_percent.Value)
        percent = Decimal(self._stop_loss_percent.Value) / Decimal(100)
        if gap >= min_gap:
            self.SellMarket(self.Volume)
            self._stop_price = candle.HighPrice * (Decimal(1) + percent)
        elif -gap >= min_gap:
            self.BuyMarket(self.Volume)
            self._stop_price = candle.LowPrice * (Decimal(1) - percent)

    def CreateClone(self):
        return overnight_gap_strategy()
