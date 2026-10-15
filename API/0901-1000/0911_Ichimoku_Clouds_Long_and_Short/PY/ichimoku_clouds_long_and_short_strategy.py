import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class ichimoku_clouds_long_and_short_strategy(Strategy):
    def __init__(self):
        super(ichimoku_clouds_long_and_short_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen period", "Indicators")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen period", "Indicators")
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span Period", "Senkou Span B period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 0.0) \
            .SetDisplay("Take Profit %", "Take profit percentage (0 - disabled)", "Risk Management")
        self._stop_loss_pct = self.Param("StopLossPct", 0.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage (0 - disabled)", "Risk Management")
        self._trade_direction = self.Param("TradeDirection", "Long") \
            .SetDisplay("Trading Mode", "Trade direction: Long or Short", "General")
        self._entry_signal_options_long = self.Param("EntrySignalOptionsLong", "Bullish All") \
            .SetDisplay("Entry Signal (Long)", "Entry signal filter for long mode", "Long Mode Signals")
        self._exit_signal_options_long = self.Param("ExitSignalOptionsLong", "Bearish Weak") \
            .SetDisplay("Exit Signal (Long)", "Exit signal filter for long mode", "Long Mode Signals")
        self._entry_signal_options_short = self.Param("EntrySignalOptionsShort", "None") \
            .SetDisplay("Entry Signal (Short)", "Entry signal filter for short mode", "Short Mode Signals")
        self._exit_signal_options_short = self.Param("ExitSignalOptionsShort", "None") \
            .SetDisplay("Exit Signal (Short)", "Exit signal filter for short mode", "Short Mode Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type for the strategy", "General")
        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ichimoku_clouds_long_and_short_strategy, self).OnReseted()
        self._prev_tenkan = 0.0
        self._prev_kijun = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(ichimoku_clouds_long_and_short_strategy, self).OnStarted2(time)
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self._tenkan_period.Value
        self._ichimoku.Kijun.Length = self._kijun_period.Value
        self._ichimoku.SenkouB.Length = self._senkou_span_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ichimoku, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _is_signal_allowed(self, option, strength, bullish):
        if option == "None":
            return False
        if bullish:
            if option == "Bullish Strong":
                return strength == "Strong"
            elif option == "Bullish Neutral":
                return strength == "Neutral"
            elif option == "Bullish Weak":
                return strength == "Weak"
            elif option == "Bullish Strong and Neutral":
                return strength in ("Strong", "Neutral")
            elif option == "Bullish Neutral and Weak":
                return strength in ("Neutral", "Weak")
            elif option == "Bullish Strong and Weak":
                return strength in ("Strong", "Weak")
            elif option == "Bullish All":
                return True
            return False
        else:
            if option == "Bearish Strong":
                return strength == "Strong"
            elif option == "Bearish Neutral":
                return strength == "Neutral"
            elif option == "Bearish Weak":
                return strength == "Weak"
            elif option == "Bearish Strong and Neutral":
                return strength in ("Strong", "Neutral")
            elif option == "Bearish Neutral and Weak":
                return strength in ("Neutral", "Weak")
            elif option == "Bearish Strong and Weak":
                return strength in ("Strong", "Weak")
            elif option == "Bearish All":
                return True
            return False

    def OnProcess(self, candle, ichimoku_val):
        if candle.State != CandleStates.Finished:
            return
        tenkan_v = ichimoku_val.Tenkan
        kijun_v = ichimoku_val.Kijun
        senkou_a = ichimoku_val.SenkouA
        senkou_b = ichimoku_val.SenkouB
        if tenkan_v is None or kijun_v is None or senkou_a is None or senkou_b is None:
            return
        tenkan = float(tenkan_v)
        kijun = float(kijun_v)
        sa = float(senkou_a)
        sb = float(senkou_b)
        upper_cloud = max(sa, sb)
        lower_cloud = min(sa, sb)
        close = float(candle.ClosePrice)
        direction = str(self._trade_direction.Value)
        cross_up = tenkan > kijun and self._prev_tenkan <= self._prev_kijun
        cross_down = tenkan < kijun and self._prev_tenkan >= self._prev_kijun
        if cross_up:
            if tenkan > upper_cloud:
                strength = "Strong"
            elif tenkan < lower_cloud:
                strength = "Weak"
            else:
                strength = "Neutral"
            if direction == "Long" and self._is_signal_allowed(str(self._entry_signal_options_long.Value), strength, True) and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
            elif direction == "Short" and self._is_signal_allowed(str(self._exit_signal_options_short.Value), strength, True) and self.Position < 0:
                self.BuyMarket()
        elif cross_down:
            if tenkan < lower_cloud:
                strength = "Strong"
            elif tenkan > upper_cloud:
                strength = "Weak"
            else:
                strength = "Neutral"
            if direction == "Short" and self._is_signal_allowed(str(self._entry_signal_options_short.Value), strength, False) and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
            elif direction == "Long" and self._is_signal_allowed(str(self._exit_signal_options_long.Value), strength, False) and self.Position > 0:
                self.SellMarket()
        tp_pct = float(self._take_profit_pct.Value)
        sl_pct = float(self._stop_loss_pct.Value)
        if self.Position > 0 and self._entry_price > 0:
            if tp_pct > 0 and close >= self._entry_price * (1.0 + tp_pct / 100.0):
                self.SellMarket()
            elif sl_pct > 0 and close <= self._entry_price * (1.0 - sl_pct / 100.0):
                self.SellMarket()
        elif self.Position < 0 and self._entry_price > 0:
            if tp_pct > 0 and close <= self._entry_price * (1.0 - tp_pct / 100.0):
                self.BuyMarket()
            elif sl_pct > 0 and close >= self._entry_price * (1.0 + sl_pct / 100.0):
                self.BuyMarket()
        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def CreateClone(self):
        return ichimoku_clouds_long_and_short_strategy()
