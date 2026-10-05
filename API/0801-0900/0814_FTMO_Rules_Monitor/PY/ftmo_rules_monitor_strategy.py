import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

MAX_DAILY_LOSS_PERCENT = 5.0
MAX_TOTAL_LOSS_PERCENT = 10.0
PROFIT_TARGET_PERCENT = 10.0
MIN_TRADING_DAYS = 4

class ftmo_rules_monitor_strategy(Strategy):
    """
    FTMO Rules Monitor strategy.
    A bullish candle goes long and a bearish candle goes short, reversing an opposite position. The volume risks RiskPercent of
    AccountSize over a stop AtrMultiplier ATRs from the entry, and that stop closes the position. The FTMO challenge rules are
    watched against AccountSize: a 5% daily loss halts trading for the day, a 10% total loss ends it, and reaching the 10% profit
    target after at least 4 trading days completes the challenge; ending or completing closes the position.
    """

    def __init__(self):
        super(ftmo_rules_monitor_strategy, self).__init__()
        self._account_size = self.Param("AccountSize", 10000.0).SetGreaterThanZero().SetDisplay("Account Size", "Challenge account size", "Challenge")
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero().SetDisplay("Risk %", "Percent of the account risked per trade", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Stop distance in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._day_start_pnl = 0.0
        self._last_trading_day = None
        self._trading_days = 0
        self._challenge_over = False
        self._stop_price = 0.0

    def OnReseted(self):
        super(ftmo_rules_monitor_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ftmo_rules_monitor_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._challenge_over:
            return

        pnl = float(self.PnL)
        account = float(self._account_size.Value)

        day = candle.OpenTime.Date
        if self._current_day != day:
            self._current_day = day
            self._day_start_pnl = pnl

        total_loss_hit = pnl <= -account * MAX_TOTAL_LOSS_PERCENT / 100.0
        target_reached = pnl >= account * PROFIT_TARGET_PERCENT / 100.0 and self._trading_days >= MIN_TRADING_DAYS

        if total_loss_hit or target_reached:
            self._challenge_over = True
            self._close_position()
            return

        if pnl - self._day_start_pnl <= -account * MAX_DAILY_LOSS_PERCENT / 100.0:
            self._close_position()
            return

        if self.Position > 0 and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket(-self.Position)
            return

        stop_distance = float(atr_value) * float(self._atr_multiplier.Value)
        if stop_distance <= 0:
            return

        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        bullish = close > open_price
        bearish = close < open_price

        if not bullish and not bearish:
            return

        if (bullish and self.Position > 0) or (bearish and self.Position < 0):
            return

        volume = self._round_volume(account * float(self._risk_percent.Value) / 100.0 / stop_distance)
        if volume <= 0:
            return

        if bullish:
            self.BuyMarket(Decimal(volume) + abs(self.Position))
            self._stop_price = close - stop_distance
        else:
            self.SellMarket(Decimal(volume) + abs(self.Position))
            self._stop_price = close + stop_distance

        if self._last_trading_day != day:
            self._last_trading_day = day
            self._trading_days += 1

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

    def _round_volume(self, volume):
        step = float(self.Security.VolumeStep) if self.Security is not None and self.Security.VolumeStep is not None else 0.0
        return math.floor(volume / step) * step if step > 0 else volume

    def CreateClone(self):
        return ftmo_rules_monitor_strategy()
