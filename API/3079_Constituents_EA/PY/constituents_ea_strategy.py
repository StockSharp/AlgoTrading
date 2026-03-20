import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class constituents_ea_strategy(Strategy):
    def __init__(self):
        super(constituents_ea_strategy, self).__init__()

        self._search_depth = self.Param("SearchDepth", 3) \
            .SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss pips", "Stop loss distance expressed in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 100.0) \
            .SetDisplay("Take Profit pips", "Take profit distance expressed in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Working timeframe used to evaluate highs and lows", "General")

        self._highest = None
        self._lowest = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._exit_requested = False

    @property
    def search_depth(self):
        return self._search_depth.Value

    @property
    def stop_loss_pips(self):
        return self._stop_loss_pips.Value

    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(constituents_ea_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._exit_requested = False

    def OnStarted(self, time):
        super(constituents_ea_strategy, self).OnStarted(time)

        step = self.Security.PriceStep if self.Security is not None else None
        self._pip_size = float(step) if step is not None and float(step) > 0 else 0.01

        self._highest = Highest()
        self._highest.Length = self.search_depth
        self._lowest = Lowest()
        self._lowest.Length = self.search_depth

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high_value = self._highest.Process(
            DecimalIndicatorValue(self._highest, candle.HighPrice, candle.OpenTime))
        low_value = self._lowest.Process(
            DecimalIndicatorValue(self._lowest, candle.LowPrice, candle.OpenTime))

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        current_high = float(high_value)
        current_low = float(low_value)

        if self.Position != 0:
            self._manage_position(candle)
            self._prev_high = current_high
            self._prev_low = current_low
            return

        close = float(candle.ClosePrice)
        sl_pips = float(self.stop_loss_pips)
        tp_pips = float(self.take_profit_pips)

        if self._prev_high > 0 and self._prev_low > 0:
            if close > self._prev_high:
                self._entry_price = close
                self._exit_requested = False
                self._stop_price = self._entry_price - sl_pips * self._pip_size if sl_pips > 0 else None
                self._take_price = self._entry_price + tp_pips * self._pip_size if tp_pips > 0 else None
                self.BuyMarket()
            elif close < self._prev_low:
                self._entry_price = close
                self._exit_requested = False
                self._stop_price = self._entry_price + sl_pips * self._pip_size if sl_pips > 0 else None
                self._take_price = self._entry_price - tp_pips * self._pip_size if tp_pips > 0 else None
                self.SellMarket()

        self._prev_high = current_high
        self._prev_low = current_low

    def _manage_position(self, candle):
        if self._exit_requested:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._take_price is not None and high >= self._take_price:
                self._exit_requested = True
                self.SellMarket()
                return
            if self._stop_price is not None and low <= self._stop_price:
                self._exit_requested = True
                self.SellMarket()
                return
        elif self.Position < 0:
            if self._take_price is not None and low <= self._take_price:
                self._exit_requested = True
                self.BuyMarket()
                return
            if self._stop_price is not None and high >= self._stop_price:
                self._exit_requested = True
                self.BuyMarket()
                return

    def CreateClone(self):
        return constituents_ea_strategy()
