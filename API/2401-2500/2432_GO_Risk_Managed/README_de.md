# GO Risk Managed-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein C#-Port des ursprünglichen MetaTrader-Skripts "GO". Sie berechnet einen benutzerdefinierten Oszillator aus gleitenden Durchschnitten der Eröffnungs-, Hoch-, Tief- und Schlusskurse und nutzt diesen zur Bestimmung der Marktrichtung.

## Strategielogik

1. Es werden vier gleitende Durchschnitte mit demselben Zeitraum und derselben Methode für die Open-, High-, Low- und Close-Reihen berechnet.
2. Der *GO*-Wert wird bei jeder abgeschlossenen Kerze berechnet:
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. Wenn der GO-Wert positiv wird, werden alle Short-Positionen geschlossen und eine neue Long-Position eröffnet.
4. Wenn der GO-Wert negativ wird, werden alle Long-Positionen geschlossen und eine neue Short-Position eröffnet.
5. Pro Kerze ist nur ein Trade erlaubt. Neue Einstiege werden vorgenommen, bis die Gesamtzahl der offenen Positionen **Max Positions** erreicht.

## Parameter

- **Risk %** – Prozentsatz des Kontokapitals, der zur Berechnung des Handelsvolumens verwendet wird.
- **Max Positions** – maximale Anzahl offener Positionen in einer Richtung.
- **MA Type** – Art des gleitenden Durchschnitts (SMA, EMA, DEMA, TEMA, WMA, VWMA).
- **MA Period** – Zeitraum für alle gleitenden Durchschnitte.
- **Candle Type** – Kerzenserie für die Indikatorberechnungen.

## Hinweise

Die Implementierung verwendet die High-Level-API von StockSharp. Sie abonniert Kerzen, bindet Indikatoren und zeichnet diese auf dem Chart. Das Handelsvolumen wird entsprechend dem angegebenen Risikoprozentsatz und den Volumenlimits des Instruments angepasst.
