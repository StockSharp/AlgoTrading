# Smart-Faktoren-Momentum-Markt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Smart Factors Momentum Market** kombiniert mehrere Aktien-Faktoren mit einem breiten Markttrend-Filter. Das System geht nur dann Long in den Markt, wenn sowohl der Faktor-Momentum-Korb als auch der Gesamtindex positive Trends aufweisen; andernfalls wird in Cash umgeschichtet.

## Details
- **Einstiegskriterien**: Bestätigung durch zusammengesetztes Faktor-Momentum und Markttrend.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Ausstieg, wenn Faktor-Momentum oder Markttrend negativ wird.
- **Stops**: Kein expliziter Stop.
- **Standardwerte**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
