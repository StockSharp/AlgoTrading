# Flachkanal-Strategie (2684)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Konvertierung des MetaTrader 5 Expert Advisors *Flat Channel (barabashkakvn's edition)*. Sie erkennt Perioden niedriger Volatilität (einen "flachen" Kanal) mit dem Standardabweichungs-Indikator und platziert Ausbruchs-Stop-Orders an den Kanalgrenzen. Wenn der Preis aus dem flachen Bereich ausbricht, wird die entsprechende Stop-Order ausgelöst, während die entgegengesetzte Order storniert wird, um zu vermeiden, auf beiden Seiten des Markts gefangen zu sein.

## Kernlogik

1. **Volatilitätsfilter** – Die Strategie abonniert Kerzen und berechnet den Standardabweichungs-Indikator des Medianpreises. Eine flache Phase wird bestätigt, wenn der Wert mindestens `FlatBars` aufeinanderfolgende Kerzen lang weiter fällt.
2. **Kanalaufbau** – Sobald die flache Phase bestätigt ist, werden das höchste Hoch und das niedrigste Tief des flachen Bereichs verfolgt. Die Kanalbreite muss zwischen `ChannelMinPips` und `ChannelMaxPips` bleiben (in Preiseinheiten über die Instrument-Tick-Größe umgerechnet).
3. **Einstiegsorders** – Solange der Preis innerhalb des Kanals handelt, platziert die Strategie:
   - Einen Buy Stop am Kanalhoch mit Stop-Loss `2 × Kanalbreite` unterhalb des Einstiegs und Take-Profit `1 × Kanalbreite` oberhalb.
   - Einen Sell Stop am Kanaltief mit den symmetrischen Stop-Loss/Take-Profit-Abständen.
4. **Order-Lebensdauer** – Ausstehende Stop-Orders laufen nach `OrderLifetimeSeconds` ab. Wenn das Timeout abläuft, werden sie storniert und können neu erstellt werden, wenn flache Bedingungen noch gelten.
5. **Positionsverwaltung** – Nachdem eine Einstiegsorder ausgeführt wurde, wird die entgegengesetzte Stop-Order storniert und neue schützende Orders (Stop-Loss und Take-Profit) werden registriert. Optionale Breakeven-Logik bewegt den Stop-Loss auf den Einstiegspreis, sobald der Preis eine Fibonacci-Fraktion (`FiboTrail`) der Distanz zum Take-Profit-Ziel zurücklegt.
6. **Handelsfenster** – Der `UseTradingHours`-Filter schränkt die Aktivität nach Wochentag und nach bestimmten Montag-/Freitag-Stunden ein und emuliert die Zeitplansteuerungen des ursprünglichen EAs.

## Indikatoren

- **StandardDeviation** (Medianpreis, Länge = `StdDevPeriod`) – erkennt fallende Volatilität.
- **DonchianChannels** (Länge = `FlatBars`) – liefert die anfänglichen Hoch/Tief-Grenzen für den flachen Kanal.

## Risiko & Geldmanagement

- `FixedVolume` definiert die Losgröße, wenn `UseMoneyManagement` deaktiviert ist.
- Wenn `UseMoneyManagement` aktiviert ist, wird die Positionsgröße aus `RiskPercent` des aktuellen Portfoliowerts geteilt durch den Stop-Loss-Abstand in Geld (unter Verwendung von `PriceStep` und `StepPrice`) geschätzt.
- Nach einem Verlust-Trade verwendet die nächste Position `FixedVolume × 4`, was das Wiederherstellungsverhalten des ursprünglichen EAs repliziert.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `UseTradingHours` | Den Wochentag/Stunden-Zeitplanfilter aktivieren oder deaktivieren. |
| `TradeTuesday`, `TradeWednesday`, `TradeThursday` | Handel an einzelnen Wochenmittel-Tagen erlauben (Montag und Freitag sind immer erlaubt, aber durch die Stundengrenzen kontrolliert). |
| `MondayStartHour`, `FridayStopHour` | Startzeit am Montag und Abbruchzeit am Freitag (24h-Uhr). |
| `UseMoneyManagement`, `RiskPercent`, `FixedVolume` | Oben beschriebene Geldmanagement-Optionen. |
| `OrderLifetimeSeconds` | Ablaufzeit für ausstehende Einstiegsorders (0 = kein Ablauf). |
| `StdDevPeriod`, `FlatBars` | Indikatoreinstellungen, die die Flachphasen-Erkennung steuern. |
| `ChannelMinPips`, `ChannelMaxPips` | Erlaubte Kanalbreite in Pips ausgedrückt (umgerechnet mit der Instrument-Tick-Größe). |
| `UseBreakeven`, `FiboTrail` | Breakeven-Logik aktivieren und den Fibonacci-Multiplikator setzen, der zur Auslösung der Stop-Anpassung verwendet wird. |
| `CandleType` | Kerzen-Datentyp oder Zeitrahmen für Berechnungen. |

## Hinweise

- Die Strategie erwartet Symbole, die `PriceStep` und `StepPrice` bereitstellen, damit Pip-basierte Schwellenwerte in tatsächliche Preise umgerechnet werden können.
- Ausstehende Orders werden nur neu erstellt, wenn die Volatilität weiter fällt. Wenn die Volatilität steigt, wird der flache Zustand zurückgesetzt und alle Einstiegsorders werden storniert.
- Schützende Stop- und Take-Profit-Orders werden automatisch storniert, wenn die Position schließt.

## Haftungsausschluss

Dieses Beispiel dient nur zu Bildungszwecken. Die vergangene Performance der ursprünglichen Strategie garantiert keine zukünftigen Ergebnisse. Teste und passe die Parameter gründlich an, bevor du auf Live-Märkten deployst.
