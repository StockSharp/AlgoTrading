import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class ma_crossover_demand_supply_zones_sltp_strategy(Strategy):
    """
    MA crossover with demand and supply zones and percent stop loss / take profit.
    The demand zone is the lowest low and the supply zone the highest high of the last ZoneLookback candles. Price is near a zone
    when the close is within ZoneStrength percent of it. A short SMA crossing above the long SMA near the demand zone goes long, a cross
    below near the supply zone goes short, reversing an opposite position. Positions exit on percent stop loss and take profit.
    """

    def __init__(self):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).__init__()
        self._short_ma_length = self.Param("ShortMaLength", 9).SetGreaterThanZero().SetDisplay("Short MA", "Short SMA period", "Indicators")
        self._long_ma_length = self.Param("LongMaLength", 21).SetGreaterThanZero().SetDisplay("Long MA", "Long SMA period", "Indicators")
        self._zone_lookback = self.Param("ZoneLookback", 50).SetGreaterThanZero().SetDisplay("Zone Lookback", "Candles used to find demand and supply zones", "Zones")
        self._zone_strength = self.Param("ZoneStrength", 2.0).SetNotNegative().SetDisplay("Zone Strength", "Distance from a zone in percent that counts as near", "Zones")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_short = None
        self._prev_long = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).OnReseted()
        self._prev_short = None
        self._prev_long = None

    def OnStarted2(self, time):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).OnStarted2(time)

        self._prev_short = None
        self._prev_long = None

        short_ma = SimpleMovingAverage()
        short_ma.Length = self._short_ma_length.Value
        long_ma = SimpleMovingAverage()
        long_ma.Length = self._long_ma_length.Value
        highest = Highest()
        highest.Length = self._zone_lookback.Value
        lowest = Lowest()
        lowest.Length = self._zone_lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_ma, long_ma, highest, lowest, self._process_candle).Start()

        tp = self._take_profit_percent.Value
        sl = self._stop_loss_percent.Value
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not long_value.IsFormed or not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        short_ma = short_value.GetValue[Decimal](None)
        long_ma = long_value.GetValue[Decimal](None)
        supply = highest_value.GetValue[Decimal](None)
        demand = lowest_value.GetValue[Decimal](None)

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = short_ma
        self._prev_long = long_ma

        if prev_short is None or prev_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        strength = Decimal(self._zone_strength.Value) / Decimal(100)
        near_demand = close <= demand * (Decimal(1) + strength)
        near_supply = close >= supply * (Decimal(1) - strength)

        cross_up = prev_short <= prev_long and short_ma > long_ma
        cross_down = prev_short >= prev_long and short_ma < long_ma

        if cross_up and near_demand and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and near_supply and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ma_crossover_demand_supply_zones_sltp_strategy()
