import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class built_in_kelly_ratio_strategy(Strategy):
    """
    Built-in Kelly Ratio strategy.
    The channel is an EMA (or SMA when UseEma is off) of Length closes plus and minus Multiplier times ATR(AtrLength). A close
    crossing above the upper band goes long and a close crossing below the lower band goes short, reversing an opposite position.
    With UseKelly the order size is Volume times the Kelly ratio W - (1 - W) / R of the closed trades (W = win rate, R = average
    win / average loss); until both a win and a loss exist the full Volume is used, and a non-positive ratio opens nothing.
    Optional percent take profit and stop loss protect the position.
    """

    def __init__(self):
        super(built_in_kelly_ratio_strategy, self).__init__()
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay("Length", "Moving average period", "Channel")
        self._multiplier = self.Param("Multiplier", 1.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiple of the band width", "Channel")
        self._atr_length = self.Param("AtrLength", 10).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Channel")
        self._use_ema = self.Param("UseEma", True).SetDisplay("Use EMA", "Use EMA instead of SMA as the channel center", "Channel")
        self._use_kelly = self.Param("UseKelly", True).SetDisplay("Use Kelly", "Size orders by the Kelly ratio", "Money Management")
        self._use_take_profit = self.Param("UseTakeProfit", False).SetDisplay("Use Take Profit", "Enable the take profit", "Risk")
        self._use_stop_loss = self.Param("UseStopLoss", False).SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk")
        self._take_profit = self.Param("TakeProfit", 10.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit in percent", "Risk")
        self._stop_loss = self.Param("StopLoss", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._last_position = Decimal(0)
        self._pnl_at_open = Decimal(0)
        self._wins = 0
        self._losses = 0
        self._gross_win = Decimal(0)
        self._gross_loss = Decimal(0)

    def OnReseted(self):
        super(built_in_kelly_ratio_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(built_in_kelly_ratio_strategy, self).OnStarted2(time)

        self._reset_state()

        ma = ExponentialMovingAverage() if self._use_ema.Value else SimpleMovingAverage()
        ma.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, atr, self._process_candle).Start()

        use_tp = self._use_take_profit.Value
        use_sl = self._use_stop_loss.Value
        tp = self._take_profit.Value
        sl = self._stop_loss.Value
        take = Unit(Decimal(tp), UnitTypes.Percent) if use_tp and tp > 0 else Unit()
        stop = Unit(Decimal(sl), UnitTypes.Percent) if use_sl and sl > 0 else Unit()
        if use_tp or use_sl:
            self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma, atr):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        multiplier = Decimal(self._multiplier.Value)
        upper = ma + atr * multiplier
        lower = ma - atr * multiplier

        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower

        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

        if prev_close is None or prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev_close <= prev_upper and close > upper and self.Position <= 0:
            volume = self._get_entry_volume() + abs(self.Position)
            if volume > 0:
                self.BuyMarket(volume)
        elif prev_close >= prev_lower and close < lower and self.Position >= 0:
            volume = self._get_entry_volume() + abs(self.Position)
            if volume > 0:
                self.SellMarket(volume)

    def _get_entry_volume(self):
        if not self._use_kelly.Value or self._wins == 0 or self._losses == 0:
            return self.Volume

        win_rate = Decimal(self._wins) / Decimal(self._wins + self._losses)
        avg_win = self._gross_win / Decimal(self._wins)
        avg_loss = self._gross_loss / Decimal(self._losses)
        if avg_loss <= 0:
            return self.Volume

        kelly = win_rate - (Decimal(1) - win_rate) / (avg_win / avg_loss)
        if kelly <= 0:
            return Decimal(0)

        volume = self.Volume * min(kelly, Decimal(1))

        security = self.Security
        if security is not None:
            step = security.VolumeStep
            if step is not None and step > 0:
                volume = Math.Floor(volume / step) * step
            min_volume = security.MinVolume
            if min_volume is not None and volume < min_volume:
                volume = min_volume

        return volume

    def OnOwnTradeReceived(self, trade):
        super(built_in_kelly_ratio_strategy, self).OnOwnTradeReceived(trade)

        position = self.Position
        last = self._last_position

        # A trade is complete when the position returns to flat or flips its side.
        if last != 0 and (position == 0 or (position > 0) != (last > 0)):
            result = self.PnL - self._pnl_at_open
            if result > 0:
                self._wins += 1
                self._gross_win += result
            elif result < 0:
                self._losses += 1
                self._gross_loss -= result
            self._pnl_at_open = self.PnL
        elif last == 0 and position != 0:
            self._pnl_at_open = self.PnL

        self._last_position = position

    def CreateClone(self):
        return built_in_kelly_ratio_strategy()
