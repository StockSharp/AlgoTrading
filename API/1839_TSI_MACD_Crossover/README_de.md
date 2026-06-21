# TSI MACD-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert ein Crossover-System basierend auf dem True Strength Index (TSI) und seiner exponentiellen gleitenden Durchschnittssignallinie.

Die Strategie abonniert standardmäßig 4-Stunden-Kerzen und berechnet den TSI mit konfigurierbaren kurzen und langen Glättungslängen. Ein zusätzlicher EMA erzeugt die Signallinie. Eine Long-Position wird eröffnet, wenn der TSI die Signallinie von unten nach oben kreuzt; eine Short-Position wird eröffnet, wenn der TSI die Signallinie von oben nach unten kreuzt. Entgegengesetzte Positionen werden beim umgekehrten Kreuz automatisch geschlossen.

- Indikatoren: True Strength Index, Exponential Moving Average
- Parameter:
  - `CandleType` – zu verarbeitende Kerzenserie.
  - `LongLength` – langer Glättungszeitraum für TSI.
  - `ShortLength` – kurzer Glättungszeitraum für TSI.
  - `SignalLength` – Periode der EMA-Signallinie.
