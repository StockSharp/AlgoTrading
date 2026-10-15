# AMkA Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie verwendet die Ableitung des Kaufman Adaptive Moving Average (KAMA) kombiniert mit einem Volatilitätsfilter auf Basis der Standardabweichung. Eine Long-Position wird eröffnet, wenn die Änderungsrate von KAMA einen dynamischen Schwellenwert überschreitet; eine Short-Position wird eröffnet, wenn sie unter den negativen Schwellenwert fällt. Der Schwellenwert wird berechnet, indem die Standardabweichung der KAMA-Änderungen mit einem benutzerdefinierten Faktor multipliziert wird.

## Parameter

- **KAMA Length** – Rückschauperiode für den KAMA-Indikator.
- **Fast Period** – schnelle EMA-Periode für die KAMA-Glättung.
- **Slow Period** – langsame EMA-Periode für die KAMA-Glättung.
- **Deviation Multiplier** – Multiplikator für die Standardabweichung zur Bildung des Signalschwellenwerts.
- **Take Profit** – Prozentsatz zur automatischen Gewinnfixierung.
- **Stop Loss** – Prozentsatz für den Schutzstop.
- **Candle Type** – Kerzen-Zeitrahmen für Berechnungen.

## Handelslogik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. KAMA für jede Kerze berechnen und die Änderung gegenüber dem Vorwert ermitteln.
3. Den Standardabweichungsindikator mit den Änderungswerten aktualisieren.
4. Wenn die Änderung `Deviation Multiplier * StdDev` überschreitet, Positionen öffnen oder schließen:
   - Änderung größer als der Schwellenwert: Short-Positionen schließen und Long öffnen.
   - Änderung kleiner als der negative Schwellenwert: Long-Positionen schließen und Short öffnen.
5. Schutzaufträge für Take-Profit und Stop-Loss werden automatisch über `StartProtection` verwaltet.

## Hinweise

Die Strategie arbeitet nur mit abgeschlossenen Kerzen und verwendet Tabulatoren zur Einrückung im Quellcode. Alle Kommentare sind auf Englisch verfasst.
