# Strategie FON60DK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Positionen, wenn die Tillson T3-Linie über das obere Band des Optimized Trend Tracker (OTT) steigt und Williams %R bullishen Schwung bestätigt. Die Position wird geschlossen, sobald Tillson T3 unter das gegenüberliegende OTT-Band fällt und Williams %R in den überverkauften Bereich eintritt.

## Details

- **Einstiegskriterien**: `T3 > OTT_up` && `Williams %R > -20`
- **Ausstiegskriterien**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Typ**: Trendfolge
- **Indikatoren**: Tillson T3, OTT, Williams %R
- **Zeitrahmen**: 1 Minute (Standard)
- **Stops**: Keine
