import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class mean_reversion_donchian_strategy(Strategy):
    """Buys at Donchian low, sells at Donchian high, targeting the midpoint."""

    def __init__(self):
        super(mean_reversion_donchian_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")
        self._lookback_period = self.Param("LookbackPeriod", 200) \
            .SetDisplay("Lookback", "Number of candles used for range detection", "Signals")
        self._risk_percent = self.Param("RiskPercent", 1.0) \
            .SetDisplay("Risk %", "Percentage of equity risked per entry", "Money Management")

        self._stop_price = None
        self._take_profit_price = None
        self._active_side = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    def OnReseted(self):
        super(mean_reversion_donchian_strategy, self).OnReseted()
        self._stop_price = None
        self._take_profit_price = None
        self._active_side = None

    def OnStarted2(self, time):
        super(mean_reversion_donchian_strategy, self).OnStarted2(time)

        donchian = DonchianChannels()
        donchian.Length = self.LookbackPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(donchian, self._process_candle).Start()

    def _process_candle(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        self._manage_open_position(candle)

        if self.Position != 0:
            return

        upper = donchian_value.UpperBand
        lower = donchian_value.LowerBand
        middle = donchian_value.Middle

        if upper is None or lower is None or middle is None:
            return

        up = float(upper)
        lo = float(lower)
        mid = float(middle)
        close = float(candle.ClosePrice)

        if float(candle.LowPrice) <= lo:
            stop_p = 2.0 * close - mid
            if stop_p < close:
                self.BuyMarket()
                self._stop_price = stop_p
                self._take_profit_price = mid
                self._active_side = Sides.Buy
        elif float(candle.HighPrice) >= up:
            stop_p = 2.0 * close - mid
            if stop_p > close:
                self.SellMarket()
                self._stop_price = stop_p
                self._take_profit_price = mid
                self._active_side = Sides.Sell

    def _manage_open_position(self, candle):
        if self.Position > 0 and self._active_side == Sides.Buy:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_state()
                return
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._reset_state()
        elif self.Position < 0 and self._active_side == Sides.Sell:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_state()
                return
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(self.Position))
                self._reset_state()

        if self.Position == 0 and self._active_side is not None:
            self._reset_state()

    def _reset_state(self):
        self._stop_price = None
        self._take_profit_price = None
        self._active_side = None

    def CreateClone(self):
        return mean_reversion_donchian_strategy()
