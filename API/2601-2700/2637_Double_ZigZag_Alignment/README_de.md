# Double ZigZag Ausrichtungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MQL5-Experten «Double ZigZag». Sie bildet die duale ZigZag-Bestätigungslogik nach, indem
zwei Swing-Detektoren mit unterschiedlichen Rückblickfenstern kombiniert werden. Ein Trade wird nur ausgelöst, wenn beide Detektoren
bei drei aufeinanderfolgenden Pivots übereinstimmen und der jüngste Swing ausreichend Stärke gegenüber den vorherigen zeigt.

## Konzept

- Der schnelle Swing-Detektor approximiert die ursprünglichen ZigZag(13, 5, 3)-Einstellungen mithilfe eines gleitenden Höchst-/Tiefstwert-Fensters.
- Der langsame Swing-Detektor verwendet ein längeres Fenster (Standard x8), um wichtige Wendepunkte zu bestätigen.
- Wenn beide Detektoren auf derselben Kerze die Richtung wechseln, wird ein «Ausrichtungs»-Pivot zusammen mit der Anzahl schneller
  Swings seit der vorherigen Ausrichtung registriert. Diese Zähler sind direkte Entsprechungen der `up`- und `dw`-Zähler des Quell-EAs.

## Long-Setup

1. Die letzte Ausrichtung ist ein Swing-Hoch, die vorherige Ausrichtung ein Swing-Tief, und die davor auch ein Swing-Hoch.
2. Die Anzahl der schnellen Swings seit der letzten Ausrichtung ist größer als der vorherige Segmentzähler multipliziert mit
   `StrengthMultiplier` (Standard 2.1). Dies emuliert die ursprüngliche Bedingung `up > dw * k`.
3. Das neueste Swing-Hoch bricht aggressiver über das mittlere Swing-Tief als das ältere Hoch, d.h. `(previousHigh - swingLow) *
   BreakoutMultiplier < (newestHigh - swingLow)` mit demselben Multiplikator-Standard von 2.1.
4. Wenn alle Kriterien erfüllt sind, kauft die Strategie ein Volumen gleich dem konfigurierten `Volume` plus etwaige offene Short-
   Positionen, sodass die Nettoposition Long wird.

## Short-Setup

1. Die letzte Ausrichtung ist ein Swing-Tief, die vorherige Ausrichtung ein Swing-Hoch, und die davor ein weiteres Swing-Tief.
2. Der jüngste Segmentzähler ist kleiner als der vorherige Zähler dividiert durch `StrengthMultiplier` (die übersetzte Prüfung
   `up * k < dw`).
3. Das aktuelle Swing-Tief bricht aggressiver unter das mittlere Swing-Hoch als das ältere Tief, unter Verwendung von `BreakoutMultiplier`.
4. Die Strategie verkauft ausreichend Volumen, um etwaige Long-Positionen zu schließen und eine Netto-Short-Position aufzubauen.

## Positionsverwaltung

- Signale schließen sich gegenseitig aus; ein neues Long schließt automatisch alle Shorts und umgekehrt.
- Es gibt keine Stop-Loss- oder Take-Profit-Orders. Positionen werden gehalten, bis ein entgegengesetztes Ausrichtungssignal erscheint.
- Die Strategie läuft auf dem durch `CandleType` angegebenen Kerzentyp (Standard: 1-Minuten-Zeitrahmen).

## Standardwerte

- `FastLength` = 13
- `SlowLength` = 104
- `StrengthMultiplier` = 2.1
- `BreakoutMultiplier` = 2.1
- `CandleType` = `TimeSpan.FromMinutes(1)` Zeitrahmen

## Tags

- **Kategorie**: Trendfolge / Mustererkennung
- **Richtung**: Long/Short
- **Indikatoren**: ZigZag (approximiert), Highest/Lowest
- **Stops**: Keine
- **Zeitrahmen**: Intraday standardmäßig
- **Komplexität**: Mittel (erfordert synchronisiertes Swing-Tracking)
- **Saisonalität**: Nein
- **Neuronale Netze**: Nein
- **Divergenz**: Nein
- **Risikolevel**: Mittel aufgrund dauerhafter Exposition ohne Stops
