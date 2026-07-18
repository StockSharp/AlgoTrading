# Bruno Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Bruno Trend Strategy ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „Bruno_v1“. Die Strategie handelt auf 30-Minuten-Kerzen und konzentriert sich auf synchronisierte bullische Signale mehrerer klassischer Trendfolge- und Momentumindikatoren. Es werden nur Long-Positionen eröffnet, was den ursprünglichen Experten nachahmt, der sich auf bullische Ausbrüche konzentrierte, die durch die Ausrichtung der Indikatoren bestätigt wurden.

## Handelslogik

1. **Zeitrahmen**: 30-Minuten-Kerzen.
2. **Indikatoren**:
   - Einfacher gleitender Durchschnitt (SMA) mit der Länge 4, der als kurzfristiger Impulsmesser verwendet wird.
   - Exponentielle gleitende Durchschnitte (EMAs) mit den Längen 8 und 21 zur Definition der primären Trendrichtung.
   - Durchschnittlicher Richtungsindex (ADX) mit Periode 13, um Richtungsstärke über +DI- und -DI-Komponenten sicherzustellen.
   - Stochastic Oszillator mit den Parametern %K=21, %D=3, Slowing=3, um die Dynamik zu bestätigen und gleichzeitig überkaufte Niveaus zu vermeiden.
   - MACD (13, 34, 8) für Histogramm und Signallinienbestätigung.
   - Parabolic SAR (Schritt 0,055, maximal 0,21), um die Aufwärtsbeschleunigung zu überprüfen und Ausgänge zu verwalten.
3. **Teilnahmebedingungen**:
   - EMA(8) muss über EMA(21) liegen.
   - ADX-Filter: +DI größer als -DI und über 20.
   - Stochastic-Filter: %K über %D, aber immer noch unter 80, um überkaufte Extreme zu vermeiden.
   - MACD Histogramm über Null und über der Signallinie.
   - Parabolic SAR steigt (aktuell SAR höher als der vorherige Messwert).
   - Die aktuelle Position muss flach oder kurz sein. Jede Short-Position wird geschlossen, bevor der neue Long-Trade eingegangen wird.
4. **Ausgangsregeln**:
   - Schließen Sie die Long-Position, wenn der vorherige Kerzenschluss unter den vorherigen Parabolic SAR-Wert fällt, wodurch der MetaTrader-Exit-Trigger reproduziert wird.

## Risikomanagement

- Standardlosgröße: 0,1 Lose.
- Optionaler Schutz im MetaTrader-Stil: 50 Pip Take-Profit und 30 Pip Stop-Loss, konfiguriert mit `StartProtection`. Nachgestellte Stopps sind standardmäßig deaktiviert, um das ursprüngliche Skript widerzuspiegeln.

## Notizen

- Die Strategie ignoriert die ungenutzte Short-Einrichtung aus dem MetaTrader-Code und entspricht dem ursprünglichen Verhalten, bei dem Short-Trades effektiv deaktiviert waren.
- Indikatorwerte werden über die übergeordnete Ebene API von StockSharp verarbeitet, um eine manuelle Pufferung zu vermeiden und mit den Projektrichtlinien in Einklang zu bleiben.
