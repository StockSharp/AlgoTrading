import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class scalp_wiz9001_strategy(Strategy):
    def __init__(self):
        super(scalp_wiz9001_strategy, self).__init__()

        self._bands_period = self.Param("BandsPeriod", 20) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
        self._bands_deviation = self.Param("BandsDeviation", 1.5) \
            .SetDisplay("Bands Deviation", "Bollinger Bands deviation", "Indicator")
        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")

        self._bollinger = None
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def bands_period(self):
        return self._bands_period.Value

    @property
    def bands_deviation(self):
        return self._bands_deviation.Value

    @property
    def stop_loss_pips(self):
        return self._stop_loss_pips.Value

    @property
    def take_profit_pips(self):
        return self._take_profit_pips.Value

    def OnReseted(self):
        super(scalp_wiz9001_strategy, self).OnReseted()
        self._bollinger = None
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(scalp_wiz9001_strategy, self).OnStarted2(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bands_period
        self._bollinger.Width = self.bands_deviation

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.BindEx(self._bollinger, self._process_candle).Start()

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return

        if not self._bollinger.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        close = float(candle.ClosePrice)
        upper_val = float(upper)
        lower_val = float(lower)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        sl_pips = self.stop_loss_pips
        tp_pips = self.take_profit_pips

        # Check SL/TP for existing long position
        if self.Position > 0 and self._entry_price > 0:
            if sl_pips > 0 and close <= self._entry_price - sl_pips * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 5
                return
            if tp_pips > 0 and close >= self._entry_price + tp_pips * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 5
                return
        elif self.Position < 0 and self._entry_price > 0:
            if sl_pips > 0 and close >= self._entry_price + sl_pips * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 5
                return
            if tp_pips > 0 and close <= self._entry_price - tp_pips * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 5
                return

        # Entry signals
        if self.Position == 0:
            if close <= lower_val:
                self.BuyMarket()
                self._entry_price = close
                self._cooldown = 5
            elif close >= upper_val:
                self.SellMarket()
                self._entry_price = close
                self._cooldown = 5

    def CreateClone(self):
        return scalp_wiz9001_strategy()
