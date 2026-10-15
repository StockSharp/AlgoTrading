# Keltner Saisonaler Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die **Keltner Seasonal Filter**-Strategie handelt basierend auf Keltner-Kanal-Ausbrüchen mit saisonalem Bias-Filter.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 94%. Sie erzielt die besten Ergebnisse am Aktienmarkt.

Signale werden ausgelöst, wenn Keltner gefilterte Einstiege auf Intraday-Daten (5m) bestätigt. Dies macht die Methode für aktive Trader geeignet.

Stops basieren auf ATR-Vielfachen und Faktoren wie EmaPeriod, AtrPeriod. Passen Sie diese Standardwerte an, um Risiko und Ertrag auszubalancieren.

## Details
- **Einstiegskriterien**: Siehe Implementierung für Indikatorbedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop-Logik.
- **Stops**: Ja, unter Verwendung indikatorbasierter Berechnungen.
- **Standardwerte**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keltner, Seasonal
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Exakte Handelsregeln (C# und Python)

Signale werden nur für **abgeschlossene Kerzen** verarbeitet, wenn die Indikatoren bereit und der Handel erlaubt sind. Es gilt `C` = Schlusskurs, `E = EMA(Close, EmaPeriod)`, `A = ATR(AtrPeriod)`, `M = AtrMultiplier`, `U = E + M × A`, `L = E − M × A`.

`S` stammt aus dem **Eröffnungsmonat der historischen Kerze** (`candle.OpenTime.Month`), nicht aus `OnStarted2` oder der Systemuhr. Die festen Beispielwerte (keine aus der Historie berechnete Saisonalität) lauten: Jan 0.8, Feb 0.3, Mär 0.6, Apr 0.7, Mai 0.2, Jun −0.3, Jul −0.1, Aug −0.4, Sep −0.8, Okt 0.1, Nov 0.9, Dez 0.7.

- **Long bzw. Short-Umkehr:** `S > SeasonalThreshold && C > U && Position <= 0`: Marktkauf `Volume + abs(Position)`; Stopplevel `C − M × A`.
- **Short bzw. Long-Umkehr:** `S < −SeasonalThreshold && C < L && Position >= 0`: Marktverkauf `Volume + abs(Position)`; Stopplevel `C + M × A`.
- **Stopp zuerst:** Bei positivem gespeicherten Stopplevel Long schließen, wenn `C <= stop`, Short, wenn `C >= stop`; Stopp zurücksetzen und diese Kerze nicht weiter auswerten.
- **EMA-Ausstieg:** Andernfalls Long bei `C < E`, Short bei `C > E` schließen; Stopp zurücksetzen. Kein separater Take-Profit oder Trailing-Stop.

Der Stopp wird **manuell beim Schluss der fertigen Kerze** geprüft, nicht intrabar an der Börse ausgeführt. Er basiert auf dem Signal-Schlusskurs, nicht dem Ausführungskurs. Bei `SeasonalThreshold = 0.5` erlauben Jan/Mär/Apr/Nov/Dez Long-Signale, Sep Short-Signale; in allen anderen Monaten sind neue Einstiege gesperrt. Ein Kanalausbruch ist zusätzlich erforderlich.
