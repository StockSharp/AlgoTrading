# Color JFATL Digit TM Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Color JFATL Digit TM Strategie** ist ein Port des ursprünglichen MetaTrader 5 Expert Advisors, der eine Jurik-gefilterte FATL (Fast Adaptive Trend Line) mit farbbasierten Zustandsübergängen und einem optionalen Trading-Sitzungsfilter kombiniert. Die Strategie überwacht die Steigung der geglätteten FATL-Linie: Jeder Balken wird als bullisch (Farbe = 2), bärisch (Farbe = 0) oder neutral (Farbe = 1) klassifiziert. Änderungen dieser Farbzustände lösen Einstiege, Ausstiege und Positionsverwaltung aus, während konfigurierbare Sitzungsstunden, Stop-Loss- und Take-Profit-Abstände berücksichtigt werden.

## Logik
1. **Replikation des benutzerdefinierten Indikators**
   - Der FATL-Wert wird durch Faltung des ausgewählten angewandten Preises mit der ursprünglichen Gewichtstabelle von 39 Koeffizienten berechnet.
   - Das Ergebnis wird mit StockSharps `JurikMovingAverage` geglättet. Wenn die Bibliothek eine `Phase`-Eigenschaft bereitstellt, wird sie durch Reflexion konfiguriert, um die MT5-Eingaben widerzuspiegeln.
   - Der geglättete Wert wird auf Instrumentenpräzision gerundet, indem der Preisschritt mit `10^DigitRounding` multipliziert wird, was den `Digit`-Parameter von MQL5 reproduziert.
   - Die Differenz zwischen dem aktuellen gerundeten Wert und dem vorherigen definiert die Farbe für den Balken (`2 = steigend`, `0 = fallend`, `1 = unverändert / geerbt`).

2. **Signalauswertung**
   - Ein Ringpuffer hält die aktuellsten Farbcodes. Der Parameter `SignalBar` legt fest, wie viele abgeschlossene Balken übersprungen werden sollen (Standard = 1, d.h. vorheriger geschlossener Balken).
   - Ein **Long-Einstieg** wird ausgelöst, wenn die vorherige Farbe bullisch war (`2`) und die jüngste Farbe alles andere als bullisch ist (`< 2`).
   - Ein **Short-Einstieg** wird ausgelöst, wenn die vorherige Farbe bärisch war (`0`) und die jüngste Farbe alles andere als bärisch ist (`> 0`).
   - Ein **Long-Ausstieg** erfolgt, wenn die vorherige Farbe bärisch wird (`0`).
   - Ein **Short-Ausstieg** erfolgt, wenn die vorherige Farbe bullisch wird (`2`).
   - Einstiege werden übersprungen, wenn bereits eine Position besteht, was das Einzelpositionsverhalten des MT5-Experten repliziert.

3. **Sitzungssteuerung und Schutz**
   - Optionale Sitzungsfilterung (`EnableTimeFilter`) spiegelt die Stunden/Minuten-Logik von MT5 wider, einschließlich Übernacht-Sitzungen, wenn die Startstunde größer als die Endstunde ist.
   - Wenn der Handel außerhalb des erlaubten Fensters liegt, werden alle offenen Positionen sofort liquidiert, was dem ursprünglichen Experten entspricht.
   - Stop-Loss- und Take-Profit-Abstände in Punkten werden in Preiseinheiten umgerechnet und an `StartProtection` übergeben.

## Parameter
- `OrderVolume` – Volumen pro Order (für Kauf- und Verkaufseinstiege).
- `EnableTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, `EndMinute` – Sitzungsfenstereinstellungen.
- `StopLossPoints`, `TakeProfitPoints` – Schutzabstände in Punkten (0 deaktiviert das jeweilige Bein).
- `BuyOpenEnabled`, `SellOpenEnabled`, `BuyCloseEnabled`, `SellCloseEnabled` – Long/Short-Einstiege und -Ausstiege einzeln aktivieren oder deaktivieren.
- `SignalCandleType` – Zeitrahmen für den benutzerdefinierten Indikator und Handelssignale (Standard 4-Stunden-Kerzen).
- `JmaLength`, `JmaPhase` – Jurik-Glättungseinstellungen (Phase wird angewendet, wenn der zugrunde liegende Indikator sie bereitstellt).
- `AppliedPriceMode` – Angewandter Preis-Enumeration identisch mit der MT5-Version (Schlusskurs, Eröffnung, Median, Typical, TrendFollow-Varianten, Demark usw.).
- `DigitRounding` – Rundungsmultiplikator, der den `Digit`-Eingabewert des MQL-Indikators imitiert.
- `SignalBar` – Wie viele geschlossene Balken beim Auswerten von Farbübergängen zurückgeschaut wird (Standard 1).

## Hinweise
- Die Strategie verwendet `SubscribeCandles` und High-Level-Order-Helfer (`BuyMarket`, `SellMarket`), wie von den StockSharp-Konvertierungsrichtlinien empfohlen.
- Die Jurik-Phase wird durch Reflexion angewendet; wenn die Laufzeitimplementierung keine `Phase`-Eigenschaft bereitstellt, wird automatisch das Standardverhalten verwendet.
- Das Runden erfordert einen gültigen `Security.PriceStep`. Wenn nicht verfügbar, bleiben Indikatorwerte ungerundet.

## Verwendung
1. Hängen Sie die Strategie an ein Wertpapier und eine Verbindung, die den konfigurierten `SignalCandleType` bereitstellen kann.
2. Konfigurieren Sie den angewandten Preis, Jurik-Parameter, Sitzungszeiten und Geldmanagement-Eingaben wie gewünscht.
3. Starten Sie die Strategie; sie verwaltet eine einzelne Position, respektiert Stop-Loss/Take-Profit-Schutz und die oben beschriebenen farbgesteuerten Signale.
