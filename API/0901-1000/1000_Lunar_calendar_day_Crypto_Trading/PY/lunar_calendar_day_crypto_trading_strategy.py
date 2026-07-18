import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lunar_calendar_day_crypto_trading_strategy(Strategy):
    SEOUL_OFFSET_HOURS = 9

    LUNAR_DATA = {
        2020: ("2020-01-25", [29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30, 29]),
        2021: ("2021-02-12", [30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30]),
        2022: ("2022-02-01", [29, 30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29]),
        2023: ("2023-01-22", [30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30]),
        2024: ("2024-02-10", [30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 30, 29]),
        2025: ("2025-01-29", [29, 30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29]),
        2026: ("2026-02-17", [30, 29, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30]),
    }

    def __init__(self):
        super(lunar_calendar_day_crypto_trading_strategy, self).__init__()
        self._buy_day = self.Param("BuyDay", 12) \
            .SetDisplay("Buy Day", "Lunar day to enter long", "Trading")
        self._sell_day = self.Param("SellDay", 26) \
            .SetDisplay("Sell Day", "Lunar day to exit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._last_trade_date = DateTimeOffset.MinValue

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lunar_calendar_day_crypto_trading_strategy, self).OnReseted()
        self._last_trade_date = DateTimeOffset.MinValue

    def OnStarted2(self, time):
        super(lunar_calendar_day_crypto_trading_strategy, self).OnStarted2(time)
        self._last_trade_date = DateTimeOffset.MinValue
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _parse_date(self, date_str):
        parts = date_str.split("-")
        return int(parts[0]), int(parts[1]), int(parts[2])

    def _get_lunar_day(self, open_time):
        try:
            year = open_time.Year
        except:
            return None
        if year not in self.LUNAR_DATA:
            return None
        date_str, lengths = self.LUNAR_DATA[year]
        sy, sm, sd = self._parse_date(date_str)
        try:
            start = DateTimeOffset(sy, sm, sd, 0, 0, 0, TimeSpan.FromHours(self.SEOUL_OFFSET_HOURS))
        except:
            return None
        if open_time.Ticks < start.Ticks:
            return None
        days = (open_time.Date - start.Date).Days
        offset = 0
        for length in lengths:
            if days < offset + length:
                return days - offset + 1
            offset += length
        return None

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        day = self._get_lunar_day(candle.OpenTime)
        if day is None:
            return
        if candle.OpenTime.Date == self._last_trade_date.Date:
            return
        if day == self._buy_day.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._last_trade_date = candle.OpenTime
        if day == self._sell_day.Value and self.Position > 0:
            self.SellMarket()
            self._last_trade_date = candle.OpenTime

    def CreateClone(self):
        return lunar_calendar_day_crypto_trading_strategy()
