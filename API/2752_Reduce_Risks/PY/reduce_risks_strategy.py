import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from System.Collections.Generic import List

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class SimpleSMA(object):
    """Manual Simple Moving Average calculator."""
    def __init__(self, length):
        self._length = length
        self._buffer = []
        self._sum = 0.0

    @property
    def IsFormed(self):
        return len(self._buffer) >= self._length

    def Process(self, value):
        self._buffer.append(value)
        self._sum += value
        if len(self._buffer) > self._length:
            self._sum -= self._buffer.pop(0)
        if self.IsFormed:
            return self._sum / self._length
        return None


class reduce_risks_strategy(Strategy):
    """Trend-following strategy using SMA hierarchy (short/medium/long) for trend detection with risk control exits."""

    def __init__(self):
        super(reduce_risks_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 30)
        self._take_profit_pips = self.Param("TakeProfitPips", 60)
        self._initial_deposit = self.Param("InitialDeposit", 1000000.0)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._sma_short = None
        self._sma_medium = None
        self._sma_long = None

        self._sma_short_curr = None
        self._sma_short_prev = None
        self._sma_medium_curr = None
        self._sma_medium_prev = None
        self._sma_long_curr = None
        self._sma_long_prev = None

        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._bars_short_above_medium = 0
        self._bars_short_below_medium = 0
        self._entered_long = False
        self._entered_short = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def InitialDeposit(self):
        return self._initial_deposit.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    def OnStarted2(self, time):
        super(reduce_risks_strategy, self).OnStarted2(time)

        self._risk_threshold = float(self.InitialDeposit) * (100.0 - float(self.RiskPercent)) / 100.0

        self._sma_short = SimpleSMA(24)
        self._sma_medium = SimpleSMA(72)
        self._sma_long = SimpleSMA(144)

        self._sma_short_curr = None
        self._sma_short_prev = None
        self._sma_medium_curr = None
        self._sma_medium_prev = None
        self._sma_long_curr = None
        self._sma_long_prev = None

        self._risk_exceeded_counter = 0
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._bars_short_above_medium = 0
        self._bars_short_below_medium = 0
        self._entered_long = False
        self._entered_short = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._sma_short is None or self._sma_medium is None or self._sma_long is None:
            return

        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        typical = (h + l + c) / 3.0

        # Update SMA short
        val_s = self._sma_short.Process(typical)
        if val_s is not None:
            self._sma_short_prev = self._sma_short_curr
            self._sma_short_curr = val_s

        # Update SMA medium
        val_m = self._sma_medium.Process(typical)
        if val_m is not None:
            self._sma_medium_prev = self._sma_medium_curr
            self._sma_medium_curr = val_m

        # Update SMA long
        val_l = self._sma_long.Process(typical)
        if val_l is not None:
            self._sma_long_prev = self._sma_long_curr
            self._sma_long_curr = val_l

        sma_s = self._sma_short_curr
        sma_m = self._sma_medium_curr
        sma_l = self._sma_long_curr

        if sma_s is None or sma_m is None or sma_l is None:
            return

        # Track consecutive bars of SMA position
        if sma_s > sma_m:
            self._bars_short_above_medium += 1
            self._bars_short_below_medium = 0
        else:
            self._bars_short_below_medium += 1
            self._bars_short_above_medium = 0

        # Risk check
        pf = self.Portfolio
        equity = float(pf.CurrentValue) if pf is not None and pf.CurrentValue is not None else float(self.InitialDeposit)
        initial_dep = float(self.InitialDeposit)
        risk_exceeded = equity <= self._risk_threshold and initial_dep > 0

        if risk_exceeded:
            if self._risk_exceeded_counter < 15:
                self._risk_exceeded_counter += 1
        else:
            self._risk_exceeded_counter = 0

        # When SMA crosses in opposite direction, allow new entry of that type
        if self._bars_short_below_medium >= 72:
            self._entered_long = False
        if self._bars_short_above_medium >= 72:
            self._entered_short = False

        pos = float(self.Position)

        if pos == 0 and not risk_exceeded:
            # LONG: short crosses above medium, not already entered on this cross
            if self._bars_short_above_medium == 1 and c > sma_s and not self._entered_long:
                self.BuyMarket()
                self._bars_since_entry = 0
                self._entered_long = True
            # SHORT: short crosses below medium, not already entered on this cross
            elif self._bars_short_below_medium == 1 and c < sma_s and not self._entered_short:
                self.SellMarket()
                self._bars_since_entry = 0
                self._entered_short = True
        elif pos != 0:
            self._bars_since_entry += 1

            if pos > 0:
                entry_price = self._entry_price
                # Exit on reverse cross after min hold
                reverse_cross = self._bars_short_below_medium >= 3 and self._bars_since_entry >= 30
                # Stop loss: 4%
                stop_loss = entry_price > 0 and c < entry_price * 0.96
                # Take profit: 6%
                take_profit = entry_price > 0 and c > entry_price * 1.06

                if reverse_cross or stop_loss or take_profit or risk_exceeded:
                    self.SellMarket(Math.Abs(self.Position))
            elif pos < 0:
                entry_price = self._entry_price
                # Exit on reverse cross after min hold
                reverse_cross = self._bars_short_above_medium >= 3 and self._bars_since_entry >= 30
                # Stop loss: 4%
                stop_loss = entry_price > 0 and c > entry_price * 1.04
                # Take profit: 6%
                take_profit = entry_price > 0 and c < entry_price * 0.94

                if reverse_cross or stop_loss or take_profit or risk_exceeded:
                    self.BuyMarket(Math.Abs(self.Position))

        if float(self.Position) == 0:
            self._entry_price = 0.0
            self._bars_since_entry = 0

    def OnOwnTradeReceived(self, trade):
        super(reduce_risks_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        if float(self.Position) != 0 and self._entry_price == 0.0:
            self._entry_price = float(trade.Trade.Price)

    def OnReseted(self):
        super(reduce_risks_strategy, self).OnReseted()
        self._sma_short = None
        self._sma_medium = None
        self._sma_long = None
        self._sma_short_curr = None
        self._sma_short_prev = None
        self._sma_medium_curr = None
        self._sma_medium_prev = None
        self._sma_long_curr = None
        self._sma_long_prev = None
        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0
        self._bars_since_entry = 0
        self._entry_price = 0.0
        self._bars_short_above_medium = 0
        self._bars_short_below_medium = 0
        self._entered_long = False
        self._entered_short = False

    def CreateClone(self):
        return reduce_risks_strategy()
