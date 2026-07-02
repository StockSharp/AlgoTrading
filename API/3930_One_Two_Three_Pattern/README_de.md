# Eins-Zwei-Drei-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie reproduziert den MetaTrader 4-Expertenberater „1-2-3_forCodeBase_v01.mq4“ von Martes. Es durchsucht fertige Kerzen nach dem klassischen 1-2-3-Umkehrmuster: zwei aufeinanderfolgende Trendabschnitte, die durch einen dritten Retracement-Abschnitt ergänzt werden. Der Port behält alle Regeln des ursprünglichen Systems bei, einschließlich der benutzerdefinierten Trendlängenindikatoren (`RelDownTrLen_forCodeBase_v01` und `RelUpTrLen_forCodeBase_v01`) und der Bestätigungslogik MACD.

Ein Long-Setup erfordert ein neues Tal (Punkt 3) in der Nähe des aktuellen Preises, einen vorhergehenden Höchststand (Punkt 2) und ein älteres Tal (Punkt 1). Der vorherige Abwärtstrend muss mindestens `TrendRatio`-mal länger sein als das aktuelle Aufwärts-Retracement, und MACD muss die Signallinie (oder Null) überschreiten und dabei bei Punkt 3 positiv bleiben. Die kurze Seite spiegelt diese Prüfungen mit umgekehrten Spitzen und Tälern wider. Stops werden einen Punkt über Punkt 3 platziert, der Take-Profit entspricht der Höhe des vorherigen Swings und ein optionaler Pip-basierter Trailing-Stop verschärft den Ausstieg, sobald der Trade in die Gewinnzone geht.

## Handelsregeln

1. Abonnieren Sie die konfigurierte Kerzenserie (`CandleType`) und berechnen Sie MACD (schnelle/langsame/Signalperioden) für die Schlusskurse.
2. Führen Sie eine fortlaufende Historie der Kerzenkörper, um die 1-2-3-Struktur zu erkennen. Täler sind lokale Minima der Kerzenkörper, Spitzen sind lokale Maxima.
3. Bewerten Sie die benutzerdefinierten Trendlängenmetriken mithilfe der Konvexhüllen-Methode anhand der MQL-Indikatoren. Die Länge des letzten Abwärtstrends (skaliert auf `[0,1]`) muss gemäß `TrendRatio` den vorhergehenden Aufwärtstrend dominieren (und umgekehrt für Shorts).
4. Bestätigen Sie die Einrichtung mit MACD:
   - Lang: `MACD` überschreitet das Signal (oder über Null) und der MACD-Wert an Punkt 3 ist positiv.
   - Kurz: `MACD` unterschreitet das Signal (oder unter Null) und der MACD-Wert an Punkt 3 ist negativ.
5. Zusätzliche Eintragsfilter:
   - Der Abstand vom aktuellen Preis zu Punkt 2 muss innerhalb von fünf Punkten liegen.
   - Der projizierte Stoppabstand (`|point2 - point3|`) muss mindestens 13 Punkte betragen.
   - `TakeProfitPips` muss ≥ 10 bleiben; andernfalls ist der Handel deaktiviert (spiegelt die ursprüngliche Sicherheitsüberprüfung wider).
6. Auftragsabwicklung:
   - Geben Sie mit `BuyMarket`/`SellMarket` mit `TradeVolume` Lots ein (aggregiert mit dem aktuellen Positionsvolumen für Umkehrungen).
   - Anfänglicher Stop-Loss = Punkt 3 ± ein Preisschritt.
   - Take Profit = Einstieg ± `|point2 - point3|`.
   - Wenn `TrailingStopPips` > 0, wird der Stopp um so viele Punkte verschoben, sobald der nicht realisierte Gewinn die Nachlaufdistanz überschreitet.
7. Ausstieg bei Stop, Take-Profit oder Trailing-Stop. Es kann jeweils nur eine Position offen sein.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `TakeProfitPips` | `decimal` | `60` | Kompatibilitätsflag von EA. Der Handel stoppt, wenn der Wert unter 10 liegt. |
| `TradeVolume` | `decimal` | `0.5` | Volumen in MetaTrader Lots, das für jede Marktorder verwendet wird. |
| `TrailingStopPips` | `decimal` | `30` | Trailing-Stop-Distanz in MetaTrader Punkten. Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `TrendRatio` | `decimal` | `4` | Minimales Verhältnis zwischen der Länge des vorherigen Haupttrends und dem jüngsten Retracement. |
| `CandleType` | `DataType` | `H1` | Kerzenserien, die für Muster- und MACD-Berechnungen verwendet werden. |
| `MacdFast` | `int` | `12` | Schnelle EMA-Periode des MACD-Oszillators. |
| `MacdSlow` | `int` | `26` | Langsame EMA-Periode des MACD-Oszillators. |
| `MacdSignal` | `int` | `9` | Signalleitung EMA Zeitraum. |
| `PatternLookback` | `int` | `100` | Maximale Anzahl historischer Kerzen, die beim Auffinden der 1-2-3-Punkte gescannt wurden. |

## Hinweise zur Implementierung

- Die ursprünglichen benutzerdefinierten Indikatoren werden wörtlich portiert: Suchen nach konvexen Hüllen berechnen die längsten monotonen Segmente von Kerzenkörpern und geben ihre relativen Längen in `[0,1]` zurück. Diese Werte steuern den Trendverhältnisfilter.
- Historische Kerzen und MACD-Werte werden in begrenzten Puffern (600 Elemente) gespeichert, um eine übermäßige Speichernutzung zu vermeiden und gleichzeitig genügend Tiefe für den Lookback beizubehalten.
- Stopps und Ziele werden manuell verwaltet, um dem MetaTrader-Verhalten zu entsprechen: Preise werden mit Kerzenhochs/-tiefs verglichen, und der Trailing-Stop verschärft sich nur, wenn der Preis mindestens um die konfigurierte Distanz steigt.
- `Volume` wird beim Zurücksetzen und beim Start mit `TradeVolume` synchronisiert, sodass sich die Optimierung auf die Standardstrategieeigenschaft stützen kann.

## Referenzen

- Ursprünglicher MQL4-Expertenberater: `MQL/8131/1-2-3_forCodeBase_v01.mq4`.
- Benutzerdefinierte Indikatoren: `RelDownTrLen_forCodeBase_v01.mq4`, `RelUpTrLen_forCodeBase_v01.mq4`.
