import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Random
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class pinball_machine_random_draw_strategy(Strategy):
    def __init__(self):
        super(pinball_machine_random_draw_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Volume submitted for every matching pair", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles that trigger random draws", "General")
        self._random_max_value = self.Param("RandomMaxValue", 99) \
            .SetDisplay("Random Maximum", "Inclusive upper bound for entry draws", "Random")
        self._min_stop_loss_points = self.Param("MinStopLossPoints", 10) \
            .SetDisplay("Minimum Stop Loss", "Minimum random stop-loss distance in price steps", "Protection")
        self._max_stop_loss_points = self.Param("MaxStopLossPoints", 50) \
            .SetDisplay("Maximum Stop Loss", "Maximum random stop-loss distance in price steps", "Protection")
        self._min_take_profit_points = self.Param("MinTakeProfitPoints", 10) \
            .SetDisplay("Minimum Take Profit", "Minimum random take-profit distance in price steps", "Protection")
        self._max_take_profit_points = self.Param("MaxTakeProfitPoints", 100) \
            .SetDisplay("Maximum Take Profit", "Maximum random take-profit distance in price steps", "Protection")
        self._random_seed = self.Param("RandomSeed", 0) \
            .SetDisplay("Random Seed", "Zero uses a time-based sequence; any other value is reproducible", "Random")

        self._random = None
        self._protected_position = 0.0
        self._protection = None

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RandomMaxValue(self):
        return self._random_max_value.Value

    @RandomMaxValue.setter
    def RandomMaxValue(self, value):
        self._random_max_value.Value = value

    @property
    def MinStopLossPoints(self):
        return self._min_stop_loss_points.Value

    @MinStopLossPoints.setter
    def MinStopLossPoints(self, value):
        self._min_stop_loss_points.Value = value

    @property
    def MaxStopLossPoints(self):
        return self._max_stop_loss_points.Value

    @MaxStopLossPoints.setter
    def MaxStopLossPoints(self, value):
        self._max_stop_loss_points.Value = value

    @property
    def MinTakeProfitPoints(self):
        return self._min_take_profit_points.Value

    @MinTakeProfitPoints.setter
    def MinTakeProfitPoints(self, value):
        self._min_take_profit_points.Value = value

    @property
    def MaxTakeProfitPoints(self):
        return self._max_take_profit_points.Value

    @MaxTakeProfitPoints.setter
    def MaxTakeProfitPoints(self, value):
        self._max_take_profit_points.Value = value

    @property
    def RandomSeed(self):
        return self._random_seed.Value

    @RandomSeed.setter
    def RandomSeed(self, value):
        self._random_seed.Value = value

    def OnReseted(self):
        super(pinball_machine_random_draw_strategy, self).OnReseted()
        self._random = None
        self._protected_position = 0.0
        self._protection = None

    def OnStarted2(self, time):
        super(pinball_machine_random_draw_strategy, self).OnStarted2(time)

        seed = int(self.RandomSeed)
        self._random = Random() if seed == 0 else Random(seed)
        self._start_protection_book()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._process_protection(candle)
        draw = self._draw_evaluation()

        if draw[0] == draw[1]:
            self._submit_entry(Sides.Buy, candle, draw[4], draw[5])

        if draw[2] == draw[3]:
            self._submit_entry(Sides.Sell, candle, draw[4], draw[5])

    def _draw_evaluation(self):
        maximum = int(self.RandomMaxValue)
        if maximum < 0:
            raise ValueError("RandomMaxValue must not be negative")

        values = [self._random.Next(maximum + 1) for _ in range(4)]
        values.append(self._draw_distance(int(self.MinStopLossPoints), int(self.MaxStopLossPoints)))
        values.append(self._draw_distance(int(self.MinTakeProfitPoints), int(self.MaxTakeProfitPoints)))
        return values

    def _draw_distance(self, minimum, maximum):
        if minimum <= 0 or maximum <= 0 or minimum > maximum:
            return 0
        return self._random.Next(minimum, maximum + 1)

    def _submit_entry(self, side, candle, stop_loss_points, take_profit_points):
        volume = self.TradeVolume
        if volume <= 0:
            return

        self._submit_market(side, volume, "Pinball entry")
        self._protected_position += volume if side == Sides.Buy else -volume

        if self._protected_position == 0:
            self._protection = None
            return

        price_step = self.Security.PriceStep if self.Security is not None else None
        if price_step is None or price_step <= 0 or (stop_loss_points <= 0 and take_profit_points <= 0):
            self._protection = None
            return

        protection = {
            "side": Sides.Buy if self._protected_position > 0 else Sides.Sell,
            "volume": abs(self._protected_position),
            "active_after": candle.CloseTime,
            "stop": None,
            "take": None,
        }
        self._set_stop_loss(protection, candle.ClosePrice, stop_loss_points, price_step)
        self._set_take_profit(protection, candle.ClosePrice, take_profit_points, price_step)
        self._protection = protection

    def _process_protection(self, candle):
        protection = self._protection
        if protection is None or candle.CloseTime <= protection["active_after"]:
            return

        is_long = protection["side"] == Sides.Buy
        stop = protection["stop"]
        take = protection["take"]
        stop_hit = stop is not None and (candle.LowPrice <= stop if is_long else candle.HighPrice >= stop)
        take_hit = not stop_hit and take is not None and (candle.HighPrice >= take if is_long else candle.LowPrice <= take)

        if not stop_hit and not take_hit:
            return

        exit_side = Sides.Sell if is_long else Sides.Buy
        self._submit_market(exit_side, protection["volume"], "Pinball protection exit")
        self._protected_position = 0.0
        self._protection = None

    def _submit_market(self, side, volume, comment):
        order = self.CreateOrder(side, 0, volume)
        order.Comment = comment
        self.RegisterOrder(order)

    def _start_protection_book(self):
        self._protected_position = self.Position
        self._protection = None

    @staticmethod
    def _set_stop_loss(protection, entry_price, points, price_step):
        if points <= 0:
            return

        distance = points * price_step
        protection["stop"] = entry_price - distance if protection["side"] == Sides.Buy else entry_price + distance

    @staticmethod
    def _set_take_profit(protection, entry_price, points, price_step):
        if points <= 0:
            return

        distance = points * price_step
        protection["take"] = entry_price + distance if protection["side"] == Sides.Buy else entry_price - distance

    def CreateClone(self):
        return pinball_machine_random_draw_strategy()
