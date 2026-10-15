# ROC2 VG Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Recreiert den MetaTrader-Expert **Exp_ROC2_VG** in StockSharp.  
Zwei Rate-of-Change-Linien mit konfigurierbaren Perioden und Berechnungstypen werden verglichen.  
Eine Long-Position wird eröffnet, wenn die erste Linie die zweite von oben nach unten kreuzt;  
eine Short-Position wird beim entgegengesetzten Kreuzung eröffnet. Die Option `Invert` tauscht die Linien aus.

## Details

- **Long-Einstieg**: vorheriges up > vorheriges down UND aktuelles up <= aktuelles down.
- **Short-Einstieg**: vorheriges up < vorheriges down UND aktuelles up >= aktuelles down.
- **Ausstieg**: Das Umkehrsignal dreht die Position sofort mit Marktorders um.
- **Zeitrahmen**: parametrisierter Kerzentyp, Standard 4 Stunden.
- **Indikatoren**: Jede Linie kann Momentum- oder ROC-artige Berechnungen verwenden:
  - Momentum = `Preis - vorheriger Preis`
  - ROC = `((Preis / vorheriger) - 1) * 100`
  - ROCP = `(Preis - vorheriger) / vorheriger`
  - ROCR = `Preis / vorheriger`
  - ROCR100 = `(Preis / vorheriger) * 100`
- **Standardparameter**:
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

Die Strategie kehrt die Positionsgröße um, wenn Signale wechseln.
