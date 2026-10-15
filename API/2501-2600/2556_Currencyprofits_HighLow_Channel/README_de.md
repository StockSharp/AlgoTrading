# Currencyprofits Hoch-Tief-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters `Currencyprofits_01.1`. Sie kombiniert einen schnellen/langsamen gleitenden Durchschnitt als Trendfilter mit einem Ausbruch aus dem jüngsten Kanalextremum. Wenn der schnelle gleitende Durchschnitt über dem langsamen liegt, erwartet die Strategie ein bullisches Umfeld und wartet darauf, dass der Preis das niedrigste Tief des vorherigen Kanalfensters retestet. Short-Trades werden eingegangen, wenn der schnelle Durchschnitt unter dem langsamen liegt und der Preis das höchste Hoch des Kanals retestet.

Die Implementierung funktioniert auf jedem Instrument, das Kerzendaten liefert. Alle Berechnungen werden auf abgeschlossenen Kerzen durchgeführt, um Stabilität sowohl in Backtests als auch im Live-Handel zu gewährleisten.

## Handelslogik
1. Abonniere den konfigurierten Kerzentyp und berechne zwei gleitende Durchschnitte und einen Donchian-Kanal auf Basis der vorherigen `ChannelLength` Kerzen (Standard: 6 Bars).
2. Speichere die vorherigen Kerzenwerte der Indikatoren, um die ursprüngliche MQL-Logik mit einem Ein-Bar-Versatz zu imitieren.
3. **Long-Einstieg**: wenn der vorherige schnelle MA größer als der vorherige langsame MA ist und das aktuelle Kerzentief den vorherigen Kanal-Tiefstwert berührt oder bricht.
4. **Short-Einstieg**: wenn der vorherige schnelle MA kleiner als der vorherige langsame MA ist und das aktuelle Kerzenhoch den vorherigen Kanal-Höchstwert berührt oder bricht.
5. **Ausstiegsregeln**:
   - Long-Positionen schließen, wenn die nächste Kerze über dem gespeicherten Kanal-Höchstwert schließt oder der Schutz-Stop erreicht wird.
   - Short-Positionen schließen, wenn die nächste Kerze unter dem gespeicherten Kanal-Tiefstwert schließt oder der Stop-Loss ausgelöst wird.
6. Es ist immer nur eine Position aktiv; die Strategie ignoriert neue Signale, während ein Trade offen ist.

## Positionsgrößenbestimmung
- `RiskPercent` definiert den Anteil des Portfoliowertes, der pro Trade riskiert werden kann (Standard `0.14`, d. h. 14%).
- Der Stop-Loss-Abstand ergibt sich aus `StopLossPoints` multipliziert mit dem `PriceStep` des Wertpapiers (oder Punkten, wenn keine Metadaten verfügbar sind).
- Das Bargeldrisiko pro Kontrakt wird mit dem Börsenschrittwert (`StepPrice`) geschätzt. Wenn das Wertpapier diese Information nicht bereitstellt, wird die rohe Preisdistanz verwendet.
- Das endgültige Ordervolumen wird an die Handelsbeschränkungen des Instruments angepasst (`VolumeStep`, `MinVolume`, `MaxVolume`). Wenn die risikobasierte Größenberechnung nicht möglich ist, wird das Basis-`Volume` der Strategie verwendet.

## Parameter
- `FastLength` – Länge des schnellen gleitenden Durchschnitts zur Trenderkennung (Standard 32).
- `FastMaType` – Typ des schnellen gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted).
- `SlowLength` – Länge des langsamen gleitenden Durchschnitts (Standard 86).
- `SlowMaType` – Typ des langsamen gleitenden Durchschnitts.
- `PriceSource` – Kerzenpreis für beide gleitende Durchschnitte (Standard Close).
- `ChannelLength` – Anzahl der vorherigen Kerzen, die den Hoch-/Tiefkanal bilden (Standard 6).
- `StopLossPoints` – Stop-Abstand in Instrumentenpunkten vor der Umrechnung in einen Preis (Standard 170).
- `RiskPercent` – Anteil des pro Trade riskierten Kapitals (Standard 0.14 → 14%).
- `CandleType` – Zeitrahmen der für alle Berechnungen verwendeten Kerzen (Standard 1 Stunde, kann geändert werden).

## Verwendungshinweise
- Sicherstellen, dass `Security.PriceStep`, `Security.StepPrice` und Volume-Metadaten für eine genaue Positionsgrößenbestimmung gefüllt sind.
- Das `Volume` der Strategie auf einen sinnvollen Fallback-Wert setzen, wenn die risikobasierte Größenberechnung deaktiviert ist (z. B. `RiskPercent = 0`).
- Die Logik handelt auf abgeschlossenen Kerzen; Live-Ausführungen erfolgen beim Kerzenschluss, der das Signal bestätigt.
- Der Stop-Loss wird intern verwaltet; es gibt keinen separaten Take-Profit, was den Quell-Expertenberater widerspiegelt.

## Quelle
Konvertiert aus `MQL/17641/Currencyprofits_01.1.mq5` mit Fokus auf Lesbarkeit und Kompatibilität mit der StockSharp High-Level-API.
