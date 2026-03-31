import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class extrem_n_strategy(Strategy):
    def __init__(self):
        super(extrem_n_strategy, self).__init__()
        self._period = self.Param("Period", 9).SetDisplay("Period", "Donchian lookback period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")
        self._buy_pos_open = self.Param("BuyPosOpen", True).SetDisplay("Buy Open", "Allow long entries", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True).SetDisplay("Sell Open", "Allow short entries", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True).SetDisplay("Buy Close", "Allow closing longs", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True).SetDisplay("Sell Close", "Allow closing shorts", "Trading")
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._up_prev = False
        self._dn_prev = False
        self._up_prev2 = False
        self._dn_prev2 = False
        self._is_first = False
    @property
    def period(self): return self._period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    @property
    def buy_pos_open(self): return self._buy_pos_open.Value
    @property
    def sell_pos_open(self): return self._sell_pos_open.Value
    @property
    def buy_pos_close(self): return self._buy_pos_close.Value
    @property
    def sell_pos_close(self): return self._sell_pos_close.Value
    def OnReseted(self):
        super(extrem_n_strategy, self).OnReseted()
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._up_prev = False
        self._dn_prev = False
        self._up_prev2 = False
        self._dn_prev2 = False
        self._is_first = False
    def OnStarted2(self, time):
        super(extrem_n_strategy, self).OnStarted2(time)
        donchian = DonchianChannels()
        donchian.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        upper = value.UpperBand
        lower = value.LowerBand
        if upper is None or lower is None: return
        upper = float(upper)
        lower = float(lower)
        if not self._is_first:
            self._prev_upper = upper
            self._prev_lower = lower
            self._is_first = True
            return
        up = float(candle.HighPrice) > self._prev_upper
        dn = float(candle.LowPrice) < self._prev_lower
        if self._up_prev2 and not self._dn_prev2:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and dn and self.Position <= 0:
                self.BuyMarket()
        elif not self._up_prev2 and self._dn_prev2:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and up and self.Position >= 0:
                self.SellMarket()
        self._up_prev2 = self._up_prev
        self._dn_prev2 = self._dn_prev
        self._up_prev = up
        self._dn_prev = dn
        self._prev_upper = upper
        self._prev_lower = lower
    def CreateClone(self): return extrem_n_strategy()
