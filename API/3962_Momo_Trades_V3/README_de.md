# Momo Trades V3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Momo Trades V3 ist eine Momentum-Strategie, die vom ursprünglichen Expertenberater MetaTrader übernommen wurde. Es kombiniert einen MACD-Musterdetektor mit mehreren Bedingungen mit einem Filter für einen verschobenen exponentiellen gleitenden Durchschnitt (EMA). Der StockSharp-Port behält die diskretionären Elemente von EA bei, fügt eine optionale Breakeven-Verarbeitung hinzu und bietet einen risikobasierten Positionsgrößenmodus, der die automatische Loslogik des Skripts widerspiegelt.

## Handelslogik
1. **MACD-Impulsmuster** – die Strategie beobachtet die Hauptlinie MACD unter Verwendung der klassischen `(12, 26, 9)`-Parameter und einer zusätzlichen Verschiebung (`MacdShift`). Zwei bullische Muster werden akzeptiert:
   - Eine streng ansteigende Sequenz, bei der der dritte Wert gleich Null ist und die folgenden beiden Abtastwerte weiter ansteigen.
   - Eine Sequenz, bei der MACD den Wert Null überschreitet, wobei die folgenden Stichproben positiv bleiben, während die vorherigen Werte negativ sind.
Bärische Einstiege erfordern die gespiegelten Bedingungen mit sinkenden Werten und einem Liniendurchgang unter Null.
2. **EMA-Distanzfilter** – der Schlusskurs des verschobenen Balkens (`MaShift`) muss mindestens `PriceShiftPoints` MetaTrader Punkte über EMA für Long-Trades und unter EMA für Short-Trades liegen. Dies vermeidet Einträge, wenn der Preis dem Durchschnitt nahe kommt.
3. **Einzelpositionsregime** – die Strategie eröffnet eine neue Position nur, wenn diese flach ist. Entgegengesetzte Signale werden ignoriert, während ein Handel aktiv ist.
4. **Session Close Exit** – wenn `CloseEndDay` aktiviert ist, liquidiert die Strategie jede Position um 23:00 Uhr Plattformzeit (21:00 Uhr an Freitagen), um ein Risiko über Nacht zu vermeiden.
5. **Optionaler Breakeven-Stopp** – wenn `UseBreakeven` aktiviert ist und sich der Preis weit genug bewegt, um einen Stop beim Einstiegspreis plus `BreakevenOffsetPoints` zu platzieren, aktiviert die Strategie ein Breakeven-Level. Wenn der Preis dann auf dieses Niveau oder darüber hinaus zurückkehrt, wird die Position zum Marktwert geschlossen.

## Risikomanagement
- **Anfangsschutz** – `StopLossPoints` und `TakeProfitPoints` werden durch den Instrumentpreisschritt in absolute Preisabstände umgewandelt und an `StartProtection` übergeben, sodass Schutzaufträge automatisch angehängt werden.
- **Auto-Volumen** – wenn `UseAutoVolume` wahr ist, wird die Ordergröße aus dem aktuellen Portfolio-Eigenkapital berechnet. Die Strategie weist dem Handel `RiskFraction` Eigenkapital zu, dividiert durch den Vertragswert (`price × lot size`), normalisiert das Ergebnis auf den Börsenvolumenschritt und respektiert `VolumeMin`/`VolumeMax`-Grenzen. Wenn die automatische Größenanpassung deaktiviert ist, wird `TradeVolume` direkt verwendet.

## Indikatoren
- **Moving Average Convergence Divergence (MACD)** – liefert das Hauptimpulssignal und wird anhand historischer Stichproben mit `MacdShift` ausgewertet.
- **Exponentieller gleitender Durchschnitt (EMA)** – wird als Filter für verschobene Trends verwendet.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeFrame(15m)` | Primärer Zeitrahmen, der für die Signalgenerierung verwendet wird. |
| `MaPeriod` | `int` | `22` | EMA Zeitraum für den Verschiebungsfilter. |
| `MaShift` | `int` | `1` | Anzahl der abgeschlossenen Balken, die beim Abtasten des Schlusskurses und EMA verwendet werden. |
| `FastPeriod` | `int` | `12` | Schnelle EMA-Länge für MACD. |
| `SlowPeriod` | `int` | `26` | Langsame EMA-Länge für MACD. |
| `SignalPeriod` | `int` | `9` | Signallänge EMA für MACD. |
| `MacdShift` | `int` | `1` | Bei der Auswertung der MACD-Muster wird eine zusätzliche Verschiebung angewendet. |
| `PriceShiftPoints` | `decimal` | `10` | Mindestabstand (in MetaTrader Punkten) zwischen dem verschobenen Schlusskurs und dem EMA, der zum Öffnen einer Position erforderlich ist. |
| `TradeVolume` | `decimal` | `0.1` | Standardhandelsvolumen, wenn die automatische Größenanpassung deaktiviert ist. |
| `RiskFraction` | `decimal` | `0.1` | Bruchteil des Portfolioeigenkapitals, der zur Größe der Order verwendet wird, wenn `UseAutoVolume` wahr ist. |
| `UseAutoVolume` | `bool` | `false` | Ermöglicht eine risikobasierte Volume-Größenbestimmung. |
| `StopLossPoints` | `decimal` | `100` | Anfängliche Stop-Loss-Distanz, ausgedrückt in MetaTrader Punkten. `0` deaktiviert den Schutzstopp. |
| `TakeProfitPoints` | `decimal` | `0` | Anfängliche Take-Profit-Distanz in MetaTrader Punkten. `0` deaktiviert das Ziel. |
| `CloseEndDay` | `bool` | `true` | Schließt offene Positionen gegen Ende des Handelstages (23:00 Uhr oder freitags 21:00 Uhr). |
| `UseBreakeven` | `bool` | `false` | Aktiviert die Breakeven-Verwaltungslogik. |
| `BreakevenOffsetPoints` | `decimal` | `0` | Offset, der dem Einstiegspreis hinzugefügt wird, wenn der Breakeven-Ausstieg aktiviert wird. |

## Nutzungshinweise
- Stellen Sie sicher, dass das Instrument über einen gültigen `PriceStep` verfügt. andernfalls greift die Strategie bei der Umwandlung von MetaTrader Punkten in Preisentfernungen auf einen Punktwert von `0.0001` zurück.
- Der MACD-Filter basiert auf fertigen Kerzen; Die Strategie wird vorzeitig beendet, damit unfertige Balken dem ursprünglichen EA-Verhalten entsprechen.
- Da jeweils nur eine Position zulässig ist, bleibt das Risiko pro Trade durch den einzelnen `TradeVolume` (oder das automatisch dimensionierte Äquivalent) kontrolliert.
