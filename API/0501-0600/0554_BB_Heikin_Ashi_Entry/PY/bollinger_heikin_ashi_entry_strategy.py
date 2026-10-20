import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Decimal, Math, TimeSpan, Convert, InvalidOperationException
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes, ITickTradeMessage

class bollinger_heikin_ashi_entry_strategy(Strategy):
    """Heikin Ashi band reversal with actual partial profit and a trailing remainder."""

    def __init__(self):
        super(bollinger_heikin_ashi_entry_strategy, self).__init__()
        self._bollingerPeriod = self.Param("BollingerPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("BollingerPeriod", "Heikin Ashi close window", "Parameters")
        self._bollingerDeviation = self.Param("BollingerDeviation", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("BollingerDeviation", "Bollinger standard deviation multiplier", "Parameters")
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Finished signal candle interval", "Parameters")
        self._clear_state()

    @property
    def BollingerPeriod(self):
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def _clear_state(self):
        self._bands = None
        self._pending = None
        self._ha_initialized = False
        self._target_taken = False
        self._invalid_fill = False
        self._ha_open = self._ha_close = Decimal(0)
        self._previous_low = self._previous_high = Decimal(0)
        self._stop = self._target = self._entry_cost = self._entry_quantity = Decimal(0)
        self._red_touches = self._green_touches = 0

    def OnReseted(self):
        super(bollinger_heikin_ashi_entry_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(bollinger_heikin_ashi_entry_strategy, self).OnStarted2(time)
        self._bands = BollingerBands()
        self._bands.Length = self.BollingerPeriod
        self._bands.Width = self.BollingerDeviation
        self.Indicators.Add(self._bands)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._bands)
            self.DrawOwnTrades(area)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Bind(self._process_quote).Start()

    def _is_pending(self):
        return self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)

    def _submit(self, side, quantity, reason):
        self._pending = self.CreateOrder(side, Decimal(0), quantity)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = "0554" + reason
        self.RegisterOrder(self._pending)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ha_open = (self._ha_open + self._ha_close) / Decimal(2) if self._ha_initialized else (candle.OpenPrice + candle.ClosePrice) / Decimal(2)
        ha_close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(4)
        self._ha_open = ha_open
        self._ha_close = ha_close
        self._ha_initialized = True
        value = DecimalIndicatorValue(self._bands, ha_close, candle.OpenTime)
        value.IsFinal = True
        bands = self._bands.Process(value)
        signal = None
        if self._bands.IsFormed and bands.LowBand is not None and bands.UpBand is not None:
            red_valid = self._red_touches >= 2
            green_valid = self._green_touches >= 2
            if ha_close > ha_open and ha_close > bands.LowBand and red_valid:
                signal = Sides.Buy
            elif ha_close < ha_open and ha_close < bands.UpBand and green_valid:
                signal = Sides.Sell
            self._red_touches = self._red_touches + 1 if ha_close < ha_open and min(candle.LowPrice, ha_open, ha_close) <= bands.LowBand else 0
            self._green_touches = self._green_touches + 1 if ha_close > ha_open and max(candle.HighPrice, ha_open, ha_close) >= bands.UpBand else 0
        if self.Position != 0 and self._target_taken:
            self._stop = max(self._stop, candle.LowPrice) if self.Position > 0 else min(self._stop, candle.HighPrice)
        setup_stop = self._previous_low if signal == Sides.Buy else self._previous_high
        self._previous_low = candle.LowPrice
        self._previous_high = candle.HighPrice
        if signal is None or self.Position != 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._stop = setup_stop
        self._entry_cost = self._entry_quantity = Decimal(0)
        self._target_taken = self._invalid_fill = False
        self._submit(signal, self.Volume, "Entry")

    def OnOwnTradeReceived(self, trade):
        super(bollinger_heikin_ashi_entry_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Comment == "0554Entry":
            price = ITickTradeMessage.Price.__get__(trade.Trade)
            quantity = ITickTradeMessage.Volume.__get__(trade.Trade)
            self._entry_cost += price * quantity
            self._entry_quantity += quantity
            entry = self._entry_cost / self._entry_quantity
            risk = entry - self._stop if trade.Order.Side == Sides.Buy else self._stop - entry
            self._invalid_fill = risk <= 0
            self._target = entry + risk if trade.Order.Side == Sides.Buy else entry - risk
        elif trade.Order.Comment == "0554Target":
            self._target_taken = True
            if self.Position != 0:
                self._stop = max(self._stop, self._previous_low) if self.Position > 0 else min(self._stop, self._previous_high)

    def _process_quote(self, quote):
        if self.Position == 0 or self._entry_quantity == 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Convert.ToDecimal(quote.Changes[field])
        if price <= 0:
            return
        side = Sides.Sell if self.Position > 0 else Sides.Buy
        if self._invalid_fill or (price <= self._stop if self.Position > 0 else price >= self._stop):
            self._submit(side, abs(self.Position), "Stop")
        elif not self._target_taken and (price >= self._target if self.Position > 0 else price <= self._target):
            quantity = abs(self.Position)
            step = self.Security.VolumeStep if self.Security.VolumeStep is not None else Decimal(1)
            minimum = self.Security.MinVolume if self.Security.MinVolume is not None else step
            half = Math.Floor(quantity / Decimal(2) / step) * step
            self._submit(side, half if half >= minimum and quantity - half >= minimum else quantity, "Target")

    def CreateClone(self):
        return bollinger_heikin_ashi_entry_strategy()
