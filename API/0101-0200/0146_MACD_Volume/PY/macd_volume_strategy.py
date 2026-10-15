import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_volume_strategy(Strategy):
    """
    MACD Volume strategy.
    A MACD cross closes a position against it. The cross opens a position in its direction only when the candle's volume exceeds
    VolumeMultiplier times the average of the previous VolumePeriod candles, so a confirmed cross reverses the position.
    A percent stop limits the loss.
    """

    def __init__(self):
        super(macd_volume_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Volume")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Volume Multiplier", "How many times the average volume a candle must exceed", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_above = None

    def OnReseted(self):
        super(macd_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume is compared with the candles before this one.
        period = self._volume_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        if not macd_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        is_above = macd_value.Macd > macd_value.Signal
        was_above = self._prev_above
        self._prev_above = is_above

        if was_above is None or was_above == is_above:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        confirmed = average is not None and candle.TotalVolume > average * Decimal(self._volume_multiplier.Value)

        if is_above:
            if confirmed and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        else:
            if confirmed and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return macd_volume_strategy()
