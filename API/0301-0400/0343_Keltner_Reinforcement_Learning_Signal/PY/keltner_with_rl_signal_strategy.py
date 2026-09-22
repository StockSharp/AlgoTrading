import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class keltner_with_rl_signal_strategy(Strategy):
    """
    Keltner with Reinforcement Learning Signal strategy.
    """

    # RL signal constants
    RL_NONE = 0
    RL_BUY = 1
    RL_SELL = 2

    def __init__(self):
        super(keltner_with_rl_signal_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for the exponential moving average", "Keltner Settings")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for the average true range", "Keltner Settings")

        self._atr_multiplier = self.Param("AtrMultiplier", 1.25) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channels", "Keltner Settings")

        self._cooldown_bars = self.Param("CooldownBars", 48) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._current_signal = self.RL_NONE
        self._last_price = 0.0
        self._previous_ema = 0.0
        self._previous_atr = 0.0
        self._previous_price = 0.0
        self._previous_signal_price = 0.0
        self._entry_price = 0.0
        self._consecutive_wins = 0
        self._consecutive_losses = 0
        self._cooldown_remaining = 0
        self._previous_above_upper = False
        self._previous_below_lower = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(keltner_with_rl_signal_strategy, self).OnReseted()
        self._current_signal = self.RL_NONE
        self._consecutive_wins = 0
        self._consecutive_losses = 0
        self._last_price = 0.0
        self._previous_ema = 0.0
        self._previous_atr = 0.0
        self._previous_price = 0.0
        self._previous_signal_price = 0.0
        self._entry_price = 0.0
        self._cooldown_remaining = 0
        self._previous_above_upper = False
        self._previous_below_lower = False

    def OnStarted2(self, time):
        super(keltner_with_rl_signal_strategy, self).OnStarted2(time)

        # Keltner Channels built from their two parts, so each documented period drives its own
        # indicator: the middle line is EMA(EmaPeriod), the band width is AtrMultiplier * ATR(AtrPeriod).
        ema = ExponentialMovingAverage()
        ema.Length = int(self._ema_period.Value)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, middle_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle_band = float(middle_value)
        current_atr = float(atr_value)

        # Keltner bands: the EMA middle line widened by the ATR of its own period
        band_offset = float(self._atr_multiplier.Value) * current_atr
        upper_band = middle_band + band_offset
        lower_band = middle_band - band_offset

        self._last_price = float(candle.ClosePrice)

        self.UpdateRLSignal(candle, middle_band, current_atr)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        price = float(candle.ClosePrice)
        price_above_upper = price > upper_band
        price_below_lower = price < lower_band
        bullish_breakout = (not self._previous_above_upper) and price_above_upper
        bearish_breakout = (not self._previous_below_lower) and price_below_lower

        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining == 0 and bullish_breakout and self._current_signal == self.RL_BUY and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._entry_price = price
            self._previous_signal_price = price
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and bearish_breakout and self._current_signal == self.RL_SELL and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._entry_price = price
            self._previous_signal_price = price
            self._cooldown_remaining = cooldown

        if self.Position > 0 and price < middle_band:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > middle_band:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = 0.0
            self._cooldown_remaining = cooldown

        self.ApplyAtrStopLoss(price, current_atr)

        self._previous_ema = middle_band
        self._previous_atr = current_atr
        self._previous_price = price
        self._previous_above_upper = price_above_upper
        self._previous_below_lower = price_below_lower

    def UpdateRLSignal(self, candle, ema, atr):
        price_above_ema = float(candle.ClosePrice) > ema
        price_increasing = float(candle.ClosePrice) > self._previous_price
        volatility_increasing = atr > self._previous_atr
        bullish_candle = candle.ClosePrice > candle.OpenPrice
        aggressive_mode = self._consecutive_wins > self._consecutive_losses

        if bullish_candle and price_above_ema and (price_increasing or aggressive_mode):
            self._current_signal = self.RL_BUY
        elif not bullish_candle and not price_above_ema and (not price_increasing or aggressive_mode):
            self._current_signal = self.RL_SELL
        else:
            if volatility_increasing:
                self._current_signal = self.RL_NONE

    def OnOwnTradeReceived(self, trade):
        if self._previous_signal_price == 0:
            return

        if trade.Order.Side == Sides.Buy:
            profitable = self._last_price > float(trade.Trade.TradePrice)
        else:
            profitable = self._last_price < float(trade.Trade.TradePrice)

        if profitable:
            self._consecutive_wins += 1
            self._consecutive_losses = 0
        else:
            self._consecutive_losses += 1
            self._consecutive_wins = 0

    def ApplyAtrStopLoss(self, price, atr):
        # Without an entry price there is no position of ours to protect
        if self._entry_price == 0.0:
            return

        # The stop stands StopLossAtr ATR multiples away from the price the position was opened at
        stop_offset = float(self._stop_loss_atr.Value) * atr

        if self.Position > 0:
            stop_level = self._entry_price - stop_offset
            if price < stop_level:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            stop_level = self._entry_price + stop_offset
            if price > stop_level:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return keltner_with_rl_signal_strategy()
