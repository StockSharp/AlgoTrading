# Revolution Volatilitätsbänder mit Range-Kontraktionssignal VII Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie baut eine Hülle um den Preis mit exponentiellen gleitenden Durchschnitten und erkennt, wenn sich der Abstand zwischen den Bändern zusammenzieht. Wenn eine Kontraktion festgestellt wird und der Preis über oder unter die geglätteten Bänder bricht, eröffnet die Strategie eine Position in Richtung des Ausbruchs.

## Details

- **Einstiegskriterien**:
  - **Long**: Range kontrahiert und Schlusskurs kreuzt über das obere geglättete Band.
  - **Short**: Range kontrahiert und Schlusskurs kreuzt unter das untere geglättete Band.
- **Ausstiegskriterien**: entgegengesetzter Ausbruch.
- **Indikatoren**: EMA-basierte Hülle.
- **Zeitrahmen**: beliebig.
