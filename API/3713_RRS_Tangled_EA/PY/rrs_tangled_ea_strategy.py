import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rrs_tangled_ea_strategy(Strategy):
    """Randomized hedging strategy - alternates buy/sell on each candle with trailing stop management."""

    def __init__(self):
        super(rrs_tangled_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series used for processing", "Data")
        self._take_profit_pips = self.Param("TakeProfitPips", 50000.0) \
            .SetDisplay("Take Profit (pips)", "Distance in pips for profit targets", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50000.0) \
            .SetDisplay("Stop Loss (pips)", "Distance in pips for protective stops", "Risk")
        self._trailing_start_pips = self.Param("TrailingStartPips", 50000.0) \
            .SetDisplay("Trailing Start (pips)", "Activation distance for trailing", "Risk")
        self._trailing_gap_pips = self.Param("TrailingGapPips", 50.0) \
            .SetDisplay("Trailing Gap (pips)", "Gap maintained by the trailing stop", "Risk")

        self._buy_entries = []
        self._sell_entries = []
        self._trade_counter = 0
        self._point = 0.0
        self._buy_trailing_stop = None
        self._sell_trailing_stop = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TrailingStartPips(self):
        return self._trailing_start_pips.Value

    @property
    def TrailingGapPips(self):
        return self._trailing_gap_pips.Value

    def OnReseted(self):
        super(rrs_tangled_ea_strategy, self).OnReseted()
        self._trade_counter = 0
        self._buy_entries = []
        self._sell_entries = []
        self._buy_trailing_stop = None
        self._sell_trailing_stop = None
        self._point = 0.0

    def OnStarted(self, time):
        super(rrs_tangled_ea_strategy, self).OnStarted(time)

        self._point = self._get_point_value()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _get_point_value(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            p = float(self.Security.PriceStep)
            if p > 0:
                return p
        return 0.0001

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)

        self._update_trailing(candle)
        self._check_stops_and_targets(candle)

        if self.Position != 0:
            return

        if self._trade_counter % 2 == 0:
            volume = float(self.Volume) if self.Volume > 0 else 1.0
            self.BuyMarket(volume)
            self._buy_entries.append([price, volume])
        else:
            volume = float(self.Volume) if self.Volume > 0 else 1.0
            self.SellMarket(volume)
            self._sell_entries.append([price, volume])

        self._trade_counter += 1

    def _update_trailing(self, candle):
        if self.TrailingStartPips <= 0 or self.TrailingGapPips <= 0:
            self._buy_trailing_stop = None
            self._sell_trailing_stop = None
            return

        bid = float(candle.ClosePrice)
        ask = float(candle.ClosePrice)
        start_dist = float(self.TrailingStartPips) * self._point
        gap_dist = float(self.TrailingGapPips) * self._point

        if len(self._buy_entries) > 0:
            avg_buy = self._get_avg_price(self._buy_entries)
            if bid - avg_buy >= start_dist:
                desired = bid - gap_dist
                if self._buy_trailing_stop is None or desired > self._buy_trailing_stop:
                    self._buy_trailing_stop = desired
                if self._buy_trailing_stop is not None and bid <= self._buy_trailing_stop:
                    self._close_buys()
        else:
            self._buy_trailing_stop = None

        if len(self._sell_entries) > 0:
            avg_sell = self._get_avg_price(self._sell_entries)
            if avg_sell - ask >= start_dist:
                desired = ask + gap_dist
                if self._sell_trailing_stop is None or desired < self._sell_trailing_stop:
                    self._sell_trailing_stop = desired
                if self._sell_trailing_stop is not None and ask >= self._sell_trailing_stop:
                    self._close_sells()
        else:
            self._sell_trailing_stop = None

    def _check_stops_and_targets(self, candle):
        stop_dist = float(self.StopLossPips) * self._point
        take_dist = float(self.TakeProfitPips) * self._point

        if len(self._buy_entries) > 0:
            avg_buy = self._get_avg_price(self._buy_entries)
            if self.StopLossPips > 0 and avg_buy - float(candle.LowPrice) >= stop_dist:
                self._close_buys()
            elif self.TakeProfitPips > 0 and float(candle.HighPrice) - avg_buy >= take_dist:
                self._close_buys()

        if len(self._sell_entries) > 0:
            avg_sell = self._get_avg_price(self._sell_entries)
            if self.StopLossPips > 0 and float(candle.HighPrice) - avg_sell >= stop_dist:
                self._close_sells()
            elif self.TakeProfitPips > 0 and avg_sell - float(candle.LowPrice) >= take_dist:
                self._close_sells()

    def _close_buys(self):
        total = sum(e[1] for e in self._buy_entries)
        if total > 0:
            self.SellMarket(total)
        self._buy_entries = []
        self._buy_trailing_stop = None

    def _close_sells(self):
        total = sum(e[1] for e in self._sell_entries)
        if total > 0:
            self.BuyMarket(total)
        self._sell_entries = []
        self._sell_trailing_stop = None

    @staticmethod
    def _get_avg_price(entries):
        total_vol = 0.0
        weighted = 0.0
        for e in entries:
            weighted += e[0] * e[1]
            total_vol += e[1]
        return weighted / total_vol if total_vol > 0 else 0.0

    def CreateClone(self):
        return rrs_tangled_ea_strategy()
