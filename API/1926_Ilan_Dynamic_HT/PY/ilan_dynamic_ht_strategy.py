import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class ilan_dynamic_ht_strategy(Strategy):

    def __init__(self):
        super(ilan_dynamic_ht_strategy, self).__init__()

        self._lot_exponent = self.Param("LotExponent", 1.4) \
            .SetDisplay("Lot Exponent", "Multiplier for next position volume", "General")
        self._max_trades = self.Param("MaxTrades", 4) \
            .SetDisplay("Max Trades", "Maximum simultaneous trades", "General")
        self._dynamic_pips = self.Param("DynamicPips", True) \
            .SetDisplay("Dynamic Range", "Use dynamic price range", "General")
        self._default_pips = self.Param("DefaultPips", 120) \
            .SetDisplay("Default Range", "Static price range in points", "General")
        self._depth = self.Param("Depth", 24) \
            .SetDisplay("Depth", "Number of bars for range calculation", "General")
        self._del = self.Param("Del", 3) \
            .SetDisplay("Divider", "Range divider factor", "General")
        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Base Volume", "Initial trade volume", "Trading")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Signals")
        self._rsi_min = self.Param("RsiMinimum", 20.0) \
            .SetDisplay("RSI Minimum", "Lower RSI bound", "Signals")
        self._rsi_max = self.Param("RsiMaximum", 80.0) \
            .SetDisplay("RSI Maximum", "Upper RSI bound", "Signals")
        self._take_profit = self.Param("TakeProfit", 100.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for processing", "General")

        self._avg_price = 0.0
        self._last_price = 0.0
        self._total_volume = 0.0
        self._trade_count = 0
        self._step = 0.0

    @property
    def LotExponent(self):
        return self._lot_exponent.Value

    @LotExponent.setter
    def LotExponent(self, value):
        self._lot_exponent.Value = value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @MaxTrades.setter
    def MaxTrades(self, value):
        self._max_trades.Value = value

    @property
    def DynamicPips(self):
        return self._dynamic_pips.Value

    @DynamicPips.setter
    def DynamicPips(self, value):
        self._dynamic_pips.Value = value

    @property
    def DefaultPips(self):
        return self._default_pips.Value

    @DefaultPips.setter
    def DefaultPips(self, value):
        self._default_pips.Value = value

    @property
    def Depth(self):
        return self._depth.Value

    @Depth.setter
    def Depth(self, value):
        self._depth.Value = value

    @property
    def Del(self):
        return self._del.Value

    @Del.setter
    def Del(self, value):
        self._del.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @BaseVolume.setter
    def BaseVolume(self, value):
        self._base_volume.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiMinimum(self):
        return self._rsi_min.Value

    @RsiMinimum.setter
    def RsiMinimum(self, value):
        self._rsi_min.Value = value

    @property
    def RsiMaximum(self):
        return self._rsi_max.Value

    @RsiMaximum.setter
    def RsiMaximum(self, value):
        self._rsi_max.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ilan_dynamic_ht_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        highest = Highest()
        highest.Length = self.Depth
        lowest = Lowest()
        lowest.Length = self.Depth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, highest, lowest, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, highest_value, lowest_value):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        high_val = float(highest_value)
        low_val = float(lowest_value)
        close = float(candle.ClosePrice)

        if self.DynamicPips:
            rng = high_val - low_val
            self._step = rng / float(self.Del) if self.Del != 0 else rng
        else:
            sec = self.Security
            price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
            self._step = float(self.DefaultPips) * price_step

        pos = self.Position

        # Entry signals
        if pos == 0:
            if rsi_val <= float(self.RsiMinimum):
                self._open_position(True, close)
            elif rsi_val >= float(self.RsiMaximum):
                self._open_position(False, close)
            return

        # Add positions when price moves against us
        if self._trade_count < self.MaxTrades:
            if pos > 0 and close <= self._last_price - self._step:
                self._add_position(True, close)
            elif pos < 0 and close >= self._last_price + self._step:
                self._add_position(False, close)

        profit = close - self._avg_price if pos > 0 else self._avg_price - close

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        tp = float(self.TakeProfit) * price_step
        sl = float(self.StopLoss) * price_step

        if profit >= tp:
            self._close_all()
        elif profit <= -sl:
            self._close_all()

    def _open_position(self, is_long, price):
        volume = float(self.BaseVolume)
        if is_long:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._avg_price = price
        self._last_price = price
        self._total_volume = volume
        self._trade_count = 1

    def _add_position(self, is_long, price):
        volume = float(self.BaseVolume) * (float(self.LotExponent) ** self._trade_count)

        if is_long:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._avg_price = (self._avg_price * self._total_volume + price * volume) / (self._total_volume + volume)
        self._total_volume += volume
        self._last_price = price
        self._trade_count += 1

    def _close_all(self):
        pos = self.Position
        if pos > 0:
            self.SellMarket(pos)
        elif pos < 0:
            self.BuyMarket(-pos)

        self._avg_price = 0.0
        self._last_price = 0.0
        self._total_volume = 0.0
        self._trade_count = 0

    def OnReseted(self):
        super(ilan_dynamic_ht_strategy, self).OnReseted()
        self._avg_price = 0.0
        self._last_price = 0.0
        self._total_volume = 0.0
        self._trade_count = 0
        self._step = 0.0

    def CreateClone(self):
        return ilan_dynamic_ht_strategy()
