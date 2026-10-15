import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_climax_reversal_strategy(Strategy):
    """
    Volume Climax Reversal strategy.
    A climax is a candle closing in the direction of the trend (a bullish candle above the SMA or a bearish one below it)
    with volume above VolumeMultiplier times the average of the previous MaPeriod candles. When the next candle retraces, the strategy
    enters against the move while flat. It exits when price closes beyond the climax extreme, when volume spikes again, or at the percent stop.
    """

    def __init__(self):
        super(volume_climax_reversal_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the trend SMA and of the volume average", "Indicators")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Volume Multiplier", "How many times the average volume a climax must exceed", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        # The climax of the previous candle: 1 up, -1 down, 0 none, and its extreme.
        self._climax = 0
        self._climax_extreme = Decimal(0)
        self._exit_level = Decimal(0)

    def OnReseted(self):
        super(volume_climax_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(volume_climax_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        # The spike is measured against the candles before this one.
        period = self._ma_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        climax = self._climax
        climax_extreme = self._climax_extreme
        self._climax = 0

        if average is None or not sma_value.IsFormed:
            return

        ma = sma_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        spike = candle.TotalVolume > average * Decimal(self._volume_multiplier.Value)

        if spike and close > candle.OpenPrice and close > ma:
            self._climax = 1
            self._climax_extreme = candle.HighPrice
        elif spike and close < candle.OpenPrice and close < ma:
            self._climax = -1
            self._climax_extreme = candle.LowPrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position < 0:
            if close > self._exit_level or spike:
                self.BuyMarket(-self.Position)
        elif self.Position > 0:
            if close < self._exit_level or spike:
                self.SellMarket(self.Position)
        elif climax == 1 and close < candle.OpenPrice:
            self.SellMarket(self.Volume)
            self._exit_level = climax_extreme
        elif climax == -1 and close > candle.OpenPrice:
            self.BuyMarket(self.Volume)
            self._exit_level = climax_extreme

    def CreateClone(self):
        return volume_climax_reversal_strategy()
