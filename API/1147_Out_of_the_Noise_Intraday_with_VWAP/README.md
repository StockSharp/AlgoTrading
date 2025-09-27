# Out of the Noise Intraday Strategy with VWAP

Implements the "Out of the Noise" intraday breakout approach. The strategy builds dynamic upper and lower bounds around the session open using average absolute moves over the past *Period* days.

Long positions are opened when price breaks above the upper bound, while short positions open below the lower bound. Existing positions exit on a VWAP cross or touch of the opposite bound. Position size can optionally scale to a volatility target derived from daily standard deviation.
