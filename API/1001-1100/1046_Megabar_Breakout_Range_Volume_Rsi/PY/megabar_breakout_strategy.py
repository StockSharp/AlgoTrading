import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class megabar_breakout_strategy(Strategy):
    """
    Megabar Breakout (Range and Volume and RSI) strategy.
    A megabar has a body larger than RangeMultiplier times the average body over RangeAveragePeriod candles and a volume
    larger than VolumeMultiplier times the average volume over VolumeAveragePeriod candles. A bullish megabar buys when the
    RsiMaPeriod moving average of RSI is above LongRsiThreshold, a bearish one sells when it is below ShortRsiThreshold,
    reversing an opposite position. Exits are a take profit and stop loss in price steps.
    """

    def __init__(self):
        super(megabar_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_average_period = self.Param("VolumeAveragePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Average Period", "Candles in the volume average", "Megabar")
        self._volume_multiplier = self.Param("VolumeMultiplier", 3.0).SetNotNegative().SetDisplay("Volume Multiplier", "Multiple of the average volume", "Megabar")
        self._range_average_period = self.Param("RangeAveragePeriod", 20).SetGreaterThanZero().SetDisplay("Range Average Period", "Candles in the body average", "Megabar")
        self._range_multiplier = self.Param("RangeMultiplier", 4.0).SetNotNegative().SetDisplay("Range Multiplier", "Multiple of the average body", "Megabar")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "RSI")
        self._rsi_ma_period = self.Param("RsiMaPeriod", 14).SetGreaterThanZero().SetDisplay("RSI MA Period", "Period of the RSI moving average", "RSI")
        self._long_rsi_threshold = self.Param("LongRsiThreshold", 50.0).SetDisplay("Long RSI Threshold", "RSI average must be above to buy", "RSI")
        self._short_rsi_threshold = self.Param("ShortRsiThreshold", 70.0).SetDisplay("Short RSI Threshold", "RSI average must be below to sell", "RSI")
        self._take_profit = self.Param("TakeProfit", 400).SetNotNegative().SetDisplay("Take Profit", "Take profit in price steps", "Risk")
        self._stop_loss = self.Param("StopLoss", 300).SetNotNegative().SetDisplay("Stop Loss", "Stop loss in price steps", "Risk")
        # Trade hours switch of the original script; the README documents no hours, so it has no effect.
        self._filter_trade_hours = self.Param("FilterTradeHours", False).SetDisplay("Filter Trade Hours", "Trade hours filter switch of the original script", "General")
        self._volume_average = None
        self._range_average = None
        self._rsi_average = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(megabar_breakout_strategy, self).OnReseted()
        self._volume_average = None
        self._range_average = None
        self._rsi_average = None

    def OnStarted2(self, time):
        super(megabar_breakout_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = self._volume_average_period.Value
        self._range_average = SimpleMovingAverage()
        self._range_average.Length = self._range_average_period.Value
        self._rsi_average = SimpleMovingAverage()
        self._rsi_average.Length = self._rsi_ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        take_steps = self._take_profit.Value
        stop_steps = self._stop_loss.Value
        take = Unit(Decimal(take_steps) * step, UnitTypes.Absolute) if take_steps > 0 else Unit()
        stop = Unit(Decimal(stop_steps) * step, UnitTypes.Absolute) if stop_steps > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True, isLocalStop=True)

        # The take profit and stop loss have to see prices between candles, not only at their close.
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
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    @staticmethod
    def _feed(average, value, time):
        indicator_input = DecimalIndicatorValue(average, value, time)
        indicator_input.IsFinal = True
        return average.Process(indicator_input).GetValue[Decimal](None)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        body = Math.Abs(candle.ClosePrice - candle.OpenPrice)
        average_volume = self._feed(self._volume_average, candle.TotalVolume, candle.OpenTime)
        average_body = self._feed(self._range_average, body, candle.OpenTime)

        if not rsi_value.IsFormed:
            return

        rsi_ma = self._feed(self._rsi_average, rsi_value.GetValue[Decimal](None), candle.OpenTime)

        if not self._volume_average.IsFormed or not self._range_average.IsFormed or not self._rsi_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        megabar = body > average_body * Decimal(self._range_multiplier.Value) and \
            candle.TotalVolume > average_volume * Decimal(self._volume_multiplier.Value)
        if not megabar:
            return

        if candle.ClosePrice > candle.OpenPrice and rsi_ma > Decimal(self._long_rsi_threshold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif candle.ClosePrice < candle.OpenPrice and rsi_ma < Decimal(self._short_rsi_threshold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return megabar_breakout_strategy()
