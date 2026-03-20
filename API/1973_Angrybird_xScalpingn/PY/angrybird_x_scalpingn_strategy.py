import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class angrybird_x_scalpingn_strategy(Strategy):

    def __init__(self):
        super(angrybird_x_scalpingn_strategy, self).__init__()

        self._lot_exponent = self.Param("LotExponent", 2.0) \
            .SetDisplay("Lot Exponent", "Volume multiplier for additional trades", "General")
        self._dynamic_pips = self.Param("DynamicPips", False) \
            .SetDisplay("Dynamic Pips", "Use dynamic grid step", "Parameters")
        self._default_pips = self.Param("DefaultPips", 12) \
            .SetDisplay("Default Pips", "Base grid step in ticks", "Parameters")
        self._depth = self.Param("Depth", 24) \
            .SetDisplay("Depth", "Bars lookback for dynamic step", "Parameters")
        self._del = self.Param("Del", 3) \
            .SetDisplay("Del", "Divider for range calculation", "Parameters")
        self._take_profit = self.Param("TakeProfit", 20.0) \
            .SetDisplay("Take Profit", "Take profit in ticks", "Risk")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")
        self._drop = self.Param("Drop", 500.0) \
            .SetDisplay("CCI Drop", "CCI threshold for exit", "Parameters")
        self._rsi_minimum = self.Param("RsiMinimum", 30.0) \
            .SetDisplay("RSI Minimum", "Minimum RSI to allow short", "Parameters")
        self._rsi_maximum = self.Param("RsiMaximum", 70.0) \
            .SetDisplay("RSI Maximum", "Maximum RSI to allow long", "Parameters")
        self._max_trades = self.Param("MaxTrades", 2) \
            .SetDisplay("Max Trades", "Maximum number of open trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")

        self._highs = deque()
        self._lows = deque()
        self._last_buy_price = None
        self._last_sell_price = None
        self._trade_count = 0
        self._pip_step = 0.0
        self._prev_close = 0.0

    @property
    def LotExponent(self):
        return self._lot_exponent.Value

    @LotExponent.setter
    def LotExponent(self, value):
        self._lot_exponent.Value = value

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
    def Drop(self):
        return self._drop.Value

    @Drop.setter
    def Drop(self, value):
        self._drop.Value = value

    @property
    def RsiMinimum(self):
        return self._rsi_minimum.Value

    @RsiMinimum.setter
    def RsiMinimum(self, value):
        self._rsi_minimum.Value = value

    @property
    def RsiMaximum(self):
        return self._rsi_maximum.Value

    @RsiMaximum.setter
    def RsiMaximum(self, value):
        self._rsi_maximum.Value = value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @MaxTrades.setter
    def MaxTrades(self, value):
        self._max_trades.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(angrybird_x_scalpingn_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        cci = CommodityChannelIndex()
        cci.Length = 55

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, cci, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, cci)

    def ProcessCandle(self, candle, rsi, cci):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rsi_val = float(rsi)
        cci_val = float(cci)

        if self.DynamicPips:
            self._highs.append(high)
            self._lows.append(low)
            if len(self._highs) > self.Depth:
                self._highs.popleft()
                self._lows.popleft()
            if len(self._highs) == self.Depth:
                highest = max(self._highs)
                lowest = min(self._lows)
                step_val = (highest - lowest) / float(self.Del)
                step_raw = self.Security.PriceStep
                step_size = float(step_raw) if step_raw is not None else 1.0
                min_step = (float(self.DefaultPips) / float(self.Del)) * step_size
                max_step = (float(self.DefaultPips) * float(self.Del)) * step_size
                step_val = min(max(step_val, min_step), max_step)
                self._pip_step = step_val
        else:
            step_raw = self.Security.PriceStep
            step_size = float(step_raw) if step_raw is not None else 1.0
            self._pip_step = float(self.DefaultPips) * step_size

        if self.Position == 0:
            self._last_buy_price = None
            self._last_sell_price = None
            self._trade_count = 0

        if self.Position > 0 and cci_val < -float(self.Drop):
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return

        if self.Position < 0 and cci_val > float(self.Drop):
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return

        if self.Position != 0:
            if self._trade_count < self.MaxTrades:
                if (self.Position > 0
                        and self._last_buy_price is not None
                        and self._last_buy_price - close >= self._pip_step):
                    vol = self.Volume * (float(self.LotExponent) ** self._trade_count)
                    self.BuyMarket(vol)
                    self._last_buy_price = close
                    self._trade_count += 1
                elif (self.Position < 0
                      and self._last_sell_price is not None
                      and close - self._last_sell_price >= self._pip_step):
                    vol = self.Volume * (float(self.LotExponent) ** self._trade_count)
                    self.SellMarket(vol)
                    self._last_sell_price = close
                    self._trade_count += 1
            self._prev_close = close
            return

        if self._prev_close != 0.0:
            if self._prev_close > close and rsi_val > float(self.RsiMinimum):
                self.SellMarket(self.Volume)
                self._last_sell_price = close
                self._trade_count = 1
            elif self._prev_close <= close and rsi_val < float(self.RsiMaximum):
                self.BuyMarket(self.Volume)
                self._last_buy_price = close
                self._trade_count = 1

        self._prev_close = close

    def OnReseted(self):
        super(angrybird_x_scalpingn_strategy, self).OnReseted()
        self._highs = deque()
        self._lows = deque()
        self._last_buy_price = None
        self._last_sell_price = None
        self._trade_count = 0
        self._pip_step = 0.0
        self._prev_close = 0.0

    def CreateClone(self):
        return angrybird_x_scalpingn_strategy()
