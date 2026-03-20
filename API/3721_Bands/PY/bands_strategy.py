import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bands_strategy(Strategy):
    def __init__(self):
        super(bands_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._bollinger_period = self.Param("BollingerPeriod", 100) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._donchian_period = self.Param("DonchianPeriod", 100) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._confirmation_period = self.Param("ConfirmationPeriod", 100) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._atr_period = self.Param("AtrPeriod", 21) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._stop_atr_multiplier = self.Param("StopAtrMultiplier", 4) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")
        self._take_atr_multiplier = self.Param("TakeAtrMultiplier", 4) \
            .SetDisplay("Volume", "Net volume in lots sent with every order", "Trading")

        self._prev_open = None
        self._prev_close = None
        self._prev_lower_band = None
        self._prev_upper_band = None
        self._prev_donch_lower = None
        self._prev_donch_upper = None
        self._prev_atr = None
        self._lower_trend_length = 0.0
        self._upper_trend_length = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._equity_samples = 0.0
        self._sum_indices = 0.0
        self._sum_equity = 0.0
        self._sum_index_equity = 0.0
        self._sum_index_squared = 0.0
        self._sum_equity_squared = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bands_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None
        self._prev_lower_band = None
        self._prev_upper_band = None
        self._prev_donch_lower = None
        self._prev_donch_upper = None
        self._prev_atr = None
        self._lower_trend_length = 0.0
        self._upper_trend_length = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._equity_samples = 0.0
        self._sum_indices = 0.0
        self._sum_equity = 0.0
        self._sum_index_equity = 0.0
        self._sum_index_squared = 0.0
        self._sum_equity_squared = 0.0

    def OnStarted(self, time):
        super(bands_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_period
        self._bollinger.Width = self.bollinger_deviation
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._donchian = DonchianChannels()
        self._donchian.Length = self.donchian_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._atr, self._donchian, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bands_strategy()
