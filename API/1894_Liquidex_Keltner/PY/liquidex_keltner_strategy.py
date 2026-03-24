import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, KeltnerChannels, RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class liquidex_keltner_strategy(Strategy):
    def __init__(self):
        super(liquidex_keltner_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 7) \
            .SetDisplay("MA Period", "Moving average period", "General")
        self._range_filter = self.Param("RangeFilter", 0.0) \
            .SetDisplay("Range Filter", "Minimum candle body", "General")
        self._stop_loss_param = self.Param("StopLoss", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
        self._take_profit_param = self.Param("TakeProfit", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
        self._use_keltner_filter = self.Param("UseKeltnerFilter", True) \
            .SetDisplay("Use Keltner", "Enable Keltner filter", "Filters")
        self._keltner_period = self.Param("KeltnerPeriod", 6) \
            .SetDisplay("Keltner Period", "Keltner period", "Filters")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 1.0) \
            .SetDisplay("Keltner Multiplier", "Keltner width", "Filters")
        self._use_rsi_filter = self.Param("UseRsiFilter", False) \
            .SetDisplay("Use RSI", "Enable RSI filter", "Filters")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Filters")
        self._entry_hour_from = self.Param("EntryHourFrom", 2) \
            .SetDisplay("Entry From", "Start hour", "Time")
        self._entry_hour_to = self.Param("EntryHourTo", 24) \
            .SetDisplay("Entry To", "End hour", "Time")
        self._friday_end_hour = self.Param("FridayEndHour", 22) \
            .SetDisplay("Friday End", "Friday closing hour", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_price = 0.0
        self._ma = None
        self._rsi = None

    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def range_filter(self):
        return self._range_filter.Value
    @property
    def stop_loss(self):
        return self._stop_loss_param.Value
    @property
    def take_profit(self):
        return self._take_profit_param.Value
    @property
    def use_keltner_filter(self):
        return self._use_keltner_filter.Value
    @property
    def keltner_period(self):
        return self._keltner_period.Value
    @property
    def keltner_multiplier(self):
        return self._keltner_multiplier.Value
    @property
    def use_rsi_filter(self):
        return self._use_rsi_filter.Value
    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def entry_hour_from(self):
        return self._entry_hour_from.Value
    @property
    def entry_hour_to(self):
        return self._entry_hour_to.Value
    @property
    def friday_end_hour(self):
        return self._friday_end_hour.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(liquidex_keltner_strategy, self).OnReseted()
        self._prev_price = 0.0

    def OnStarted(self, time):
        super(liquidex_keltner_strategy, self).OnStarted(time)
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.ma_period
        keltner = KeltnerChannels()
        keltner.Length = self.keltner_period
        keltner.Multiplier = float(self.keltner_multiplier)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Percent),
            Unit(float(self.stop_loss), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            if self.use_keltner_filter:
                self.DrawIndicator(area, keltner)
            if self.use_rsi_filter:
                self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _process_indicator(self, indicator, price, open_time):
        inp = DecimalIndicatorValue(indicator, price, open_time)
        inp.IsFinal = True
        return indicator.Process(inp)

    def process_candle(self, candle, keltner_value):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        t = candle.CloseTime

        # process MA and RSI manually
        ma_result = self._process_indicator(self._ma, candle.ClosePrice, candle.OpenTime)
        rsi_result = self._process_indicator(self._rsi, candle.ClosePrice, candle.OpenTime)

        if not self._is_trading_time(t):
            self._prev_price = price
            return

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        if body < float(self.range_filter):
            self._prev_price = price
            return

        if not ma_result.IsFinal or not ma_result.IsFormed:
            self._prev_price = price
            return

        ma = float(ma_result)
        rsi_val = float(rsi_result) if rsi_result.IsFormed else 50.0

        if self.use_keltner_filter:
            upper_val = keltner_value.Upper
            lower_val = keltner_value.Lower
            if upper_val is None or lower_val is None:
                self._prev_price = price
                return
            upper = float(upper_val)
            lower = float(lower_val)

            cross_above = self._prev_price > 0 and self._prev_price <= upper and price > upper
            cross_below = self._prev_price > 0 and self._prev_price >= lower and price < lower

            if cross_above and price > ma and (not self.use_rsi_filter or rsi_val > 50.0) and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif cross_below and price < ma and (not self.use_rsi_filter or rsi_val < 50.0) and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        else:
            if price > ma and (not self.use_rsi_filter or rsi_val > 50.0) and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif price < ma and (not self.use_rsi_filter or rsi_val < 50.0) and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_price = price

    def _is_trading_time(self, t):
        hour = t.Hour
        if t.DayOfWeek == DayOfWeek.Friday and hour >= self.friday_end_hour:
            return False
        if self.entry_hour_from <= self.entry_hour_to:
            return hour >= self.entry_hour_from and hour <= self.entry_hour_to
        return hour >= self.entry_hour_from or hour <= self.entry_hour_to

    def CreateClone(self):
        return liquidex_keltner_strategy()
