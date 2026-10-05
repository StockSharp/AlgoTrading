import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class multi_timeframe_parabolic_sar_strategy(Strategy):
    """
    Multi-timeframe Parabolic SAR strategy.
    Parabolic SAR is calculated on the main, a higher and a lower timeframe. A long opens when the main candle closes above every SAR
    selected by LongSource, a short when it closes below every SAR selected by ShortSource, reversing an opposite position.
    A position closes when price crosses the main timeframe SAR the other way. Percent stop loss and take profit are handled by
    protection and a percent trailing stop follows the best price since entry.
    LongSource/ShortSource: Current, CurrentAndHigher, CurrentAndLower or All.
    """

    def __init__(self):
        super(multi_timeframe_parabolic_sar_strategy, self).__init__()
        self._acceleration = self.Param("Acceleration", 0.02).SetGreaterThanZero().SetDisplay("Acceleration", "Initial acceleration factor", "Indicators")
        self._max_acceleration = self.Param("MaxAcceleration", 0.2).SetGreaterThanZero().SetDisplay("Max Acceleration", "Maximum acceleration factor", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._trailing_percent = self.Param("TrailingPercent", 0.5).SetNotNegative().SetDisplay("Trailing %", "Trailing stop percentage from the best price", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._long_source = self.Param("LongSource", "All").SetDisplay("Long Source", "SAR levels price must be above for a long", "Signals")
        self._short_source = self.Param("ShortSource", "All").SetDisplay("Short Source", "SAR levels price must be below for a short", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Main timeframe", "General")
        self._higher_candle_type = self.Param("HigherCandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("Higher Candle Type", "Higher timeframe", "General")
        self._lower_candle_type = self.Param("LowerCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Lower Candle Type", "Lower timeframe", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self._higher_candle_type.Value), (self.Security, self._lower_candle_type.Value)]

    def _reset_state(self):
        self._higher_sar = None
        self._higher_close = None
        self._lower_sar = None
        self._lower_close = None
        self._best_price = 0.0

    def OnReseted(self):
        super(multi_timeframe_parabolic_sar_strategy, self).OnReseted()
        self._reset_state()

    def _create_sar(self):
        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._acceleration.Value)
        sar.AccelerationStep = Decimal(self._acceleration.Value)
        sar.AccelerationMax = Decimal(self._max_acceleration.Value)
        return sar

    def OnStarted2(self, time):
        super(multi_timeframe_parabolic_sar_strategy, self).OnStarted2(time)

        self._reset_state()

        sar = self._create_sar()
        self._higher_ind = self._create_sar()
        self._lower_ind = self._create_sar()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self._process_candle).Start()
        self.SubscribeCandles(self._higher_candle_type.Value).Bind(self._higher_ind, self._process_higher).Start()
        self.SubscribeCandles(self._lower_candle_type.Value).Bind(self._lower_ind, self._process_lower).Start()

        tp = float(self._take_profit_percent.Value)
        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True, isLocalStop=True)

        # The stops have to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_higher(self, candle, value):
        if candle.State != CandleStates.Finished or not self._higher_ind.IsFormed:
            return
        self._higher_sar = float(value)
        self._higher_close = float(candle.ClosePrice)

    def _process_lower(self, candle, value):
        if candle.State != CandleStates.Finished or not self._lower_ind.IsFormed:
            return
        self._lower_sar = float(value)
        self._lower_close = float(candle.ClosePrice)

    def _uses(self, source):
        source = str(source)
        return (source in ("CurrentAndHigher", "All"), source in ("CurrentAndLower", "All"))

    def _is_above(self, source, close, sar):
        if close <= sar:
            return False
        use_higher, use_lower = self._uses(source)
        if use_higher and (self._higher_sar is None or self._higher_close <= self._higher_sar):
            return False
        if use_lower and (self._lower_sar is None or self._lower_close <= self._lower_sar):
            return False
        return True

    def _is_below(self, source, close, sar):
        if close >= sar:
            return False
        use_higher, use_lower = self._uses(source)
        if use_higher and (self._higher_sar is None or self._higher_close >= self._higher_sar):
            return False
        if use_lower and (self._lower_sar is None or self._lower_close >= self._lower_sar):
            return False
        return True

    def _process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        sar = float(sar_value)
        trailing = float(self._trailing_percent.Value)

        if self._is_above(self._long_source.Value, close, sar) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._best_price = close
            return

        if self._is_below(self._short_source.Value, close, sar) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._best_price = close
            return

        if self.Position > 0:
            self._best_price = max(self._best_price, float(candle.HighPrice))
            if close < sar or (trailing > 0 and close <= self._best_price * (1.0 - trailing / 100.0)):
                self.SellMarket(self.Position)
        elif self.Position < 0:
            low = float(candle.LowPrice)
            self._best_price = low if self._best_price == 0 else min(self._best_price, low)
            if close > sar or (trailing > 0 and close >= self._best_price * (1.0 + trailing / 100.0)):
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return multi_timeframe_parabolic_sar_strategy()
