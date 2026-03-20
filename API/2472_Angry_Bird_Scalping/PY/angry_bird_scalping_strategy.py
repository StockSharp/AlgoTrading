import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex, Highest, Lowest, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class angry_bird_scalping_strategy(Strategy):
    def __init__(self):
        super(angry_bird_scalping_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 500)
        self._take_profit = self.Param("TakeProfit", 40)
        self._default_pips = self.Param("DefaultPips", 20)
        self._depth = self.Param("Depth", 24)
        self._lot_exponent = self.Param("LotExponent", 1.62)
        self._max_trades = self.Param("MaxTrades", 3)
        self._rsi_min = self.Param("RsiMin", 70.0)
        self._rsi_max = self.Param("RsiMax", 30.0)
        self._cci_drop = self.Param("CciDrop", 500.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._last_open_buy_price = 0.0
        self._last_open_sell_price = 0.0
        self._entry_price = 0.0
        self._trade_count = 0
        self._long_trade = False
        self._short_trade = False
        self._rsi_value = 0.0
        self._prev_close = None

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

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
    def RsiMin(self):
        return self._rsi_min.Value

    @RsiMin.setter
    def RsiMin(self, value):
        self._rsi_min.Value = value

    @property
    def RsiMax(self):
        return self._rsi_max.Value

    @RsiMax.setter
    def RsiMax(self, value):
        self._rsi_max.Value = value

    @property
    def CciDrop(self):
        return self._cci_drop.Value

    @CciDrop.setter
    def CciDrop(self, value):
        self._cci_drop.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(angry_bird_scalping_strategy, self).OnStarted(time)

        self._trade_count = 0
        self._long_trade = False
        self._short_trade = False
        self._entry_price = 0.0

        cci = CommodityChannelIndex()
        cci.Length = 55
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        highest = Highest()
        highest.Length = self.Depth
        lowest = Lowest()
        lowest.Length = self.Depth

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, rsi, highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, cci_value, rsi_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        cci_val = float(cci_value)
        rsi_val = float(rsi_value)
        high_val = float(highest_value)
        low_val = float(lowest_value)

        step_price = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        pip_distance = max((high_val - low_val) / max(step_price, 1.0), float(self.DefaultPips)) * step_price

        self._rsi_value = rsi_val

        cci_drop = float(self.CciDrop)
        if (cci_val > cci_drop and self._short_trade) or (cci_val < -cci_drop and self._long_trade):
            self._close_all()
            return

        trade_now = False

        if self.Position == 0:
            self._trade_count = 0
            self._long_trade = False
            self._short_trade = False
            trade_now = True
        elif self._trade_count < int(self.MaxTrades):
            if self._long_trade and self._last_open_buy_price - close >= pip_distance:
                trade_now = True
            if self._short_trade and close - self._last_open_sell_price >= pip_distance:
                trade_now = True

        if trade_now:
            if self._long_trade:
                self.BuyMarket()
                self._last_open_buy_price = close
                self._trade_count += 1
            elif self._short_trade:
                self.SellMarket()
                self._last_open_sell_price = close
                self._trade_count += 1
            elif self._prev_close is not None and self._prev_close > close:
                rsi_min = float(self.RsiMin)
                rsi_max = float(self.RsiMax)
                if self._rsi_value > rsi_min:
                    self.SellMarket()
                    self._short_trade = True
                    self._last_open_sell_price = close
                    self._entry_price = close
                    self._trade_count = 1
                elif self._rsi_value < rsi_max:
                    self.BuyMarket()
                    self._long_trade = True
                    self._last_open_buy_price = close
                    self._entry_price = close
                    self._trade_count = 1

        if self.Position != 0:
            sl = float(self.StopLoss)
            tp = float(self.TakeProfit)
            if self._long_trade:
                tp_price = self._entry_price + tp * step_price
                sl_price = self._entry_price - sl * step_price
                if close >= tp_price or close <= sl_price:
                    self._close_all()
            elif self._short_trade:
                tp_price = self._entry_price - tp * step_price
                sl_price = self._entry_price + sl * step_price
                if close <= tp_price or close >= sl_price:
                    self._close_all()

        self._prev_close = close

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()
        self._trade_count = 0
        self._long_trade = False
        self._short_trade = False
        self._entry_price = 0.0

    def OnReseted(self):
        super(angry_bird_scalping_strategy, self).OnReseted()
        self._last_open_buy_price = 0.0
        self._last_open_sell_price = 0.0
        self._entry_price = 0.0
        self._trade_count = 0
        self._long_trade = False
        self._short_trade = False
        self._rsi_value = 0.0
        self._prev_close = None

    def CreateClone(self):
        return angry_bird_scalping_strategy()
