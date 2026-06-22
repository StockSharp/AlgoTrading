# Strategie Öffnung und Schließung zur festgelegten Zeit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine einfache zeitbasierte Strategie, die zu einer bestimmten Tageszeit eine Marktposition eröffnet und sie zu einer anderen vordefinierten Zeit schließt. Die Richtung (Kauf oder Verkauf) und das Ordervolumen sind konfigurierbar. Dieses Beispiel demonstriert die geplante Handelsausführung ohne Indikatoren oder zusätzliche Filter.

## Details

- **Einstiegskriterien**:
  - **Long**: Zum Zeitpunkt `Open Time`, wenn `Is Buy` aktiviert ist.
  - **Short**: Zum Zeitpunkt `Open Time`, wenn `Is Buy` deaktiviert ist.
- **Long/Short**: Beide, abhängig von `Is Buy`.
- **Ausstiegskriterien**:
  - Position wird zu `Close Time` unabhängig von Gewinn oder Verlust geschlossen.
- **Stops**: Keine.
- **Standardwerte**:
  - `Open Time` = 13:00.
  - `Close Time` = 13:01.
  - `Volume` = 1.
  - `Is Buy` = true.
  - `Candle Type` = 1 Minute.
- **Filter**:
  - Kategorie: Zeit
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
