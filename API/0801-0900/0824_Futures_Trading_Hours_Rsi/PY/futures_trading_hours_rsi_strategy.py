import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, TimeZoneInfo, TimeZoneNotFoundException, DateTime, DateTimeKind
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class futures_trading_hours_rsi_strategy(Strategy):
    """
    Futures Trading Hours RSI strategy.
    Trades only between SessionStart and SessionEnd in US Central Time. Inside the session an RSI crossing above OverSoldLevel goes
    long and an RSI crossing below OverBoughtLevel goes short, reversing an opposite position. Any position still open at or after
    SessionEnd is closed.
    """

    def __init__(self):
        super(futures_trading_hours_rsi_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._over_sold_level = self.Param("OverSoldLevel", 30.0).SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._over_bought_level = self.Param("OverBoughtLevel", 70.0).SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._session_start = self.Param("SessionStart", TimeSpan(8, 30, 0)).SetDisplay("Session Start", "Session start in US Central Time", "Session")
        self._session_end = self.Param("SessionEnd", TimeSpan(15, 0, 0)).SetDisplay("Session End", "Session end in US Central Time", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_rsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(futures_trading_hours_rsi_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(futures_trading_hours_rsi_strategy, self).OnStarted2(time)

        self._prev_rsi = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _central_time_zone(self):
        try:
            return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago")
        except TimeZoneNotFoundException:
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)
        last_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        utc = DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc)
        minutes = TimeZoneInfo.ConvertTimeFromUtc(utc, self._central_time_zone()).TimeOfDay.TotalMinutes
        start = self._session_start.Value.TotalMinutes
        end = self._session_end.Value.TotalMinutes

        if minutes >= end or minutes < start:
            if minutes >= end:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                elif self.Position < 0:
                    self.BuyMarket(-self.Position)
            return

        if last_rsi is None:
            return

        oversold = float(self._over_sold_level.Value)
        overbought = float(self._over_bought_level.Value)

        if last_rsi <= oversold and rsi > oversold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif last_rsi >= overbought and rsi < overbought and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return futures_trading_hours_rsi_strategy()
