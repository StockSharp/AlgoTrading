# Voss Predictor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy implements John Ehlers' Voss predictive filter with a band-pass filter to anticipate price movement. A long position opens when the predictive filter rises above the band-pass output, while a short position opens when it falls below.

## Details

- **Entry**: Voss predictive filter crosses above the band-pass filter.
- **Exit**: Voss predictive filter crosses below the band-pass filter.
- **Type**: Trend following.
- **Stop**: None.
