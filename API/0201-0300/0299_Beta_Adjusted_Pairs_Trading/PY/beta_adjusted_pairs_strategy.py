import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, Decimal, Math, TimeSpan, Convert, InvalidOperationException
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription, Security, Portfolio, Order
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes, ITickTradeMessage

class beta_adjusted_pairs_strategy(Strategy):
    """Synchronized beta-neutral pair, return toward the mean, and a two-leg filled loss limit."""

    def __init__(self):
        super(beta_adjusted_pairs_strategy, self).__init__()
        self._asset2 = self.Param[Security]("Asset2").SetRequired() \
            .SetDisplay("Asset2", "Explicit second execution instrument", "Parameters")
        self._asset2Portfolio = self.Param[Portfolio]("Asset2Portfolio").SetRequired() \
            .SetDisplay("Asset2Portfolio", "Explicit portfolio for the hedge leg", "Parameters")
        self._betaAsset1 = self.Param("BetaAsset1", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("BetaAsset1", "Positive primary beta", "Parameters")
        self._betaAsset2 = self.Param("BetaAsset2", Decimal(1)).SetGreaterThanZero() \
            .SetDisplay("BetaAsset2", "Positive secondary beta", "Parameters")
        self._lookbackPeriod = self.Param("LookbackPeriod", 20).SetRange(2, 100000) \
            .SetDisplay("LookbackPeriod", "Matched spread observations in the z-score window", "Parameters")
        self._entryThreshold = self.Param("EntryThreshold", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("EntryThreshold", "Extreme z-score boundary to cross back toward zero", "Parameters")
        self._exitThreshold = self.Param("ExitThreshold", Decimal(0.15)).SetNotNegative() \
            .SetDisplay("ExitThreshold", "Mean-reversion exit band", "Parameters")
        self._stopLoss = self.Param("StopLoss", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("StopLoss", "Loss limit as a percentage of gross filled entry notional", "Parameters")
        self._cooldownBars = self.Param("CooldownBars", 120).SetNotNegative() \
            .SetDisplay("CooldownBars", "Finished matched bars between pair entries", "Parameters")
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("CandleType", "Same finished candle interval on both legs", "Parameters")
        self._clear_state()

    @property
    def Asset2(self):
        return self._asset2.Value

    @Asset2.setter
    def Asset2(self, value):
        self._asset2.Value = value

    @property
    def Asset2Portfolio(self):
        return self._asset2Portfolio.Value

    @Asset2Portfolio.setter
    def Asset2Portfolio(self, value):
        self._asset2Portfolio.Value = value

    @property
    def BetaAsset1(self):
        return self._betaAsset1.Value

    @BetaAsset1.setter
    def BetaAsset1(self, value):
        self._betaAsset1.Value = value

    @property
    def BetaAsset2(self):
        return self._betaAsset2.Value

    @BetaAsset2.setter
    def BetaAsset2(self, value):
        self._betaAsset2.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def EntryThreshold(self):
        return self._entryThreshold.Value

    @EntryThreshold.setter
    def EntryThreshold(self, value):
        self._entryThreshold.Value = value

    @property
    def ExitThreshold(self):
        return self._exitThreshold.Value

    @ExitThreshold.setter
    def ExitThreshold(self, value):
        self._exitThreshold.Value = value

    @property
    def StopLoss(self):
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CooldownBars(self):
        return self._cooldownBars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldownBars.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def _clear_state(self):
        self._first_bars = {}
        self._second_bars = {}
        self._spreads = []
        self._orders = []
        self._last_pair = DateTime.MinValue
        self._base1 = self._base2 = self._previous_z = None
        self._cost1 = self._cost2 = self._quantity1 = self._quantity2 = self._primary_quantity = self._hedge_quantity = Decimal(0)
        self._side = self._bar = 0
        self._last_entry = None

    def OnReseted(self):
        super(beta_adjusted_pairs_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Asset2, self.CandleType)]

    def OnStarted2(self, time):
        if self.Asset2 is None or self.Asset2Portfolio is None or self.Security is None or self.Asset2.Id.lower() == self.Security.Id.lower():
            raise InvalidOperationException("Asset2 must explicitly identify a different instrument, and Asset2Portfolio is required.")
        if self.ExitThreshold >= self.EntryThreshold:
            raise InvalidOperationException("ExitThreshold must be smaller than EntryThreshold.")
        super(beta_adjusted_pairs_strategy, self).OnStarted2(time)
        first = self.SubscribeCandles(self.CandleType)
        second = self.SubscribeCandles(self.CandleType, security=self.Asset2)
        first.Bind(self._process_first).Start()
        second.Bind(self._process_second).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, first)
            self.DrawCandles(area, second)
            self.DrawOwnTrades(area)

    def _process_first(self, candle):
        self._process_candle(candle, self._first_bars)

    def _process_second(self, candle):
        self._process_candle(candle, self._second_bars)

    def _position2(self):
        value = self.GetPositionValue(self.Asset2, self.Asset2Portfolio)
        return Decimal(0) if value is None else value

    def _process_candle(self, candle, buffer):
        if candle.State != CandleStates.Finished or candle.ClosePrice <= 0 or candle.OpenTime <= self._last_pair:
            return
        time = candle.OpenTime
        buffer[time] = candle.ClosePrice
        while len(buffer) > self.LookbackPeriod * 2:
            del buffer[min(buffer)]
        if time not in self._first_bars or time not in self._second_bars:
            return
        first = self._first_bars[time]
        second = self._second_bars[time]
        self._last_pair = time
        for source in (self._first_bars, self._second_bars):
            for old in list(source):
                if old <= time:
                    del source[old]
        self._bar += 1
        if self._base1 is None:
            self._base1, self._base2 = first, second
        beta = self.BetaAsset1 / self.BetaAsset2
        spread = Decimal(Math.Log(float(first / self._base1))) - beta * Decimal(Math.Log(float(second / self._base2)))
        self._spreads.append(spread)
        if len(self._spreads) > self.LookbackPeriod:
            self._spreads.pop(0)
        if len(self._spreads) < self.LookbackPeriod:
            return
        mean = sum(self._spreads, Decimal(0)) / Decimal(self.LookbackPeriod)
        variance = sum(((v - mean) * (v - mean) for v in self._spreads), Decimal(0)) / Decimal(self.LookbackPeriod)
        deviation = Decimal(Math.Sqrt(float(variance)))
        previous = self._previous_z
        z = (spread - mean) / deviation if deviation > 0 else None
        self._previous_z = z
        if not self.IsFormedAndOnlineAndAllowTrading() or any(o.State not in (OrderStates.Done, OrderStates.Failed) for o in self._orders):
            return
        self._orders = []
        position1, position2 = self.Position, self._position2()
        if position1 != 0 or position2 != 0:
            if self._side == 0 or position1 != Decimal(self._side) * self._primary_quantity or position2 != -Decimal(self._side) * self._hedge_quantity:
                self._close_pair("Unbalanced")
                return
            profit = Decimal(self._side) * (self._quantity1 * first - self._cost1 - self._quantity2 * second + self._cost2)
            if self._cost1 + self._cost2 > 0 and profit <= -(self._cost1 + self._cost2) * self.StopLoss / Decimal(100):
                self._close_pair("Stop")
            elif z is not None and (z >= -self.ExitThreshold if self._side > 0 else z <= self.ExitThreshold):
                self._close_pair("Mean")
            return
        self._side = 0
        if previous is None or z is None or self._last_entry is not None and self._bar - self._last_entry <= self.CooldownBars:
            return
        side = 1 if previous <= -self.EntryThreshold and z > -self.EntryThreshold and z < 0 else -1 if previous >= self.EntryThreshold and z < self.EntryThreshold and z > 0 else 0
        if side == 0:
            return
        step = self.Asset2.VolumeStep if self.Asset2.VolumeStep is not None else Decimal(1)
        self._hedge_quantity = Math.Floor(self.Volume * first * beta / second / step) * step
        minimum = self.Asset2.MinVolume if self.Asset2.MinVolume is not None else step
        if self._hedge_quantity < minimum or self.Asset2.MaxVolume is not None and self._hedge_quantity > self.Asset2.MaxVolume:
            raise InvalidOperationException("The beta-neutral hedge quantity is outside Asset2 lot limits.")
        self._cost1 = self._cost2 = self._quantity1 = self._quantity2 = Decimal(0)
        self._side = side
        self._primary_quantity = self.Volume
        self._last_entry = self._bar
        self._submit(self.Security, self.Portfolio, Decimal(side) * self._primary_quantity, "Entry")
        self._submit(self.Asset2, self.Asset2Portfolio, -Decimal(side) * self._hedge_quantity, "Entry")

    def _submit(self, security, portfolio, quantity, reason):
        if quantity == 0:
            return
        order = Order()
        order.Security = security
        order.Portfolio = portfolio
        order.Side = Sides.Buy if quantity > 0 else Sides.Sell
        order.Volume = abs(quantity)
        order.Type = OrderTypes.Market
        order.Comment = "0299" + reason
        self._orders.append(order)
        self.RegisterOrder(order)

    def _close_pair(self, reason):
        self._submit(self.Security, self.Portfolio, -self.Position, reason)
        self._submit(self.Asset2, self.Asset2Portfolio, -self._position2(), reason)
        self._side = 0

    def OnOwnTradeReceived(self, trade):
        super(beta_adjusted_pairs_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Comment != "0299Entry":
            return
        quantity = ITickTradeMessage.Volume.__get__(trade.Trade)
        price = ITickTradeMessage.Price.__get__(trade.Trade)
        if trade.Order.Security == self.Security:
            self._quantity1 += quantity
            self._cost1 += quantity * price
        else:
            self._quantity2 += quantity
            self._cost2 += quantity * price

    def CreateClone(self):
        return beta_adjusted_pairs_strategy()
