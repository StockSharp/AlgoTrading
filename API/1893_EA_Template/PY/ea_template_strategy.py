import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ea_template_strategy(Strategy):
    def __init__(self):
        super(ea_template_strategy, self).__init__()
        self._reverse_trade = self.Param("ReverseTrade", False) \
            .SetDisplay("Reverse Trade", "Invert entry and exit signals", "Trading")
        self._use_money_management = self.Param("UseMoneyManagement", False) \
            .SetDisplay("Use Money Management", "Use risk based volume calculation", "Risk Management")
        self._risk_percent = self.Param("RiskPercent", 30.0) \
            .SetDisplay("Risk Percent", "Percent of equity to risk", "Risk Management")
        self._lots = self.Param("Lots", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Fixed Lot Size", "Fixed order size", "Risk Management")
        self._stop_loss_param = self.Param("StopLoss", 50) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
        self._take_profit_param = self.Param("TakeProfit", 70) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk Management")
        self._spread_limit = self.Param("SpreadLimit", 10) \
            .SetDisplay("Spread Limit", "Maximum spread in points", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._entry_price = 0.0
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._bars_since_trade = 0

    @property
    def reverse_trade(self):
        return self._reverse_trade.Value

    @property
    def use_money_management(self):
        return self._use_money_management.Value

    @property
    def risk_percent(self):
        return self._risk_percent.Value

    @property
    def lots(self):
        return self._lots.Value

    @property
    def stop_loss(self):
        return self._stop_loss_param.Value

    @property
    def take_profit(self):
        return self._take_profit_param.Value

    @property
    def spread_limit(self):
        return self._spread_limit.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ea_template_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._bars_since_trade = self.cooldown_bars

    def OnStarted2(self, time):
        super(ea_template_strategy, self).OnStarted2(time)
        self.StartProtection(None, None)
        sma = SimpleMovingAverage()
        sma.Length = 50
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma)
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        is_bullish = close > open_price
        is_bearish = close < open_price
        self._bars_since_trade += 1

        if sma_val == 0.0:
            return

        rev = self.reverse_trade

        if self.Position == 0:
            cross_above = (self._prev_close != 0.0 and self._prev_sma != 0.0 and
                           self._prev_close <= self._prev_sma and close > sma_val)
            cross_below = (self._prev_close != 0.0 and self._prev_sma != 0.0 and
                           self._prev_close >= self._prev_sma and close < sma_val)
            buy_signal = (self._bars_since_trade >= self.cooldown_bars and
                          ((is_bullish and cross_above and not rev) or (is_bearish and cross_below and rev)))
            sell_signal = (self._bars_since_trade >= self.cooldown_bars and
                           ((is_bearish and cross_below and not rev) or (is_bullish and cross_above and rev)))

            if buy_signal:
                self.BuyMarket()
                self._entry_price = close
                self._bars_since_trade = 0
            elif sell_signal:
                self.SellMarket()
                self._entry_price = close
                self._bars_since_trade = 0

            self._prev_close = close
            self._prev_sma = sma_val
            return

        exit_long = (is_bearish and not rev) or (is_bullish and rev)
        exit_short = (is_bullish and not rev) or (is_bearish and rev)

        if self.Position > 0 and exit_long:
            self.SellMarket()
        if self.Position < 0 and exit_short:
            self.BuyMarket()

        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        step = float(step)

        if self.Position > 0:
            stop = self.stop_loss * step
            if stop > 0 and close <= self._entry_price - stop:
                self.SellMarket()
            else:
                profit = self.take_profit * step
                if profit > 0 and close >= self._entry_price + profit:
                    self.SellMarket()
        elif self.Position < 0:
            stop = self.stop_loss * step
            if stop > 0 and close >= self._entry_price + stop:
                self.BuyMarket()
            else:
                profit = self.take_profit * step
                if profit > 0 and close <= self._entry_price - profit:
                    self.BuyMarket()

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return ea_template_strategy()
