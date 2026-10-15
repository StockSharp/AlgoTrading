# Tuyul Unzensierte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Tuyul Uncensored ist eine Swing-Following-Strategie, die den ursprünglichen MetaTrader 5 Expert Advisor mit dem High-Level API von StockSharp nachbildet. Das System beobachtet ZigZag-Schwankungen, gleicht Einträge mit einem gleitenden Durchschnittstrendfilter aus und platziert Limit-Orders beim 57 % Fibonacci-Retracement des letzten Abschnitts. Wenn der Preis dieses Niveau erneut erreicht, versucht die Strategie, sich dem vorherrschenden Schwung anzuschließen und gleichzeitig den Handel mit Stop-Loss- und Take-Profit-Niveaus zu schützen, die von demselben Bein abgeleitet sind.

## Handelslogik
1. **Datenvorbereitung**
   - Ein Kerzenabonnement, definiert durch den ausgewählten Parameter `Candle Type`.
   - Ein ZigZag-Indikator (Tiefe/Abweichung/Rückschritt) wird verwendet, um das letzte bestätigte Swing-Hoch und Swing-Tief zu verfolgen.
   - Schnelle und langsame EMAs (Standard 9/21) stellen den Richtungsfilter bereit.
2. **Signalerkennung**
   - Wenn der ZigZag einen neuen Pivot bestätigt (entweder ein neues Hoch oder ein neues Tief), aktualisiert die Strategie das jüngste Swing-Paar.
   - Wenn keine Orders aktiv sind und keine offene Position vorhanden ist, bestimmen die vorherigen EMA-Werte den Trend:
     - Schneller EMA über langsamer EMA → bullischer Kontext.
     - Schneller EMA unter langsamer EMA → bärischer Kontext.
3. **Auftragserteilung**
   - In einem bullischen Kontext platziert die Strategie eine **Kauflimit**-Order beim 57 %-Retracement zwischen dem letzten Swing-Tief und dem Swing-Hoch.
   - In einem rückläufigen Kontext wird eine **Verkaufslimit**-Order beim symmetrischen 57 %-Retracement vom Swing-Hoch zum Swing-Tief platziert.
   - Stop-Loss ist am entgegengesetzten ZigZag-Extrem verankert, während Take-Profit der Stop-Distanz multipliziert mit `Take Profit Multiplier` (Standard 1,2) entspricht.
   - Bestellungen bleiben für `Wait Bars After Signal` Kerzen aktiv; Danach werden sie abgebrochen, um auf ein neues Signal zu warten.
4. **Positionsverwaltung**
   - Sobald eine Order ausgeführt wird, überwacht die Strategie die nachfolgenden Kerzen. Eine Long-Position wird geschlossen, wenn der Preis entweder den vordefinierten Stop-Loss oder Take-Profit erreicht. Die gleiche gespiegelte Logik gilt für Short-Positionen.
   - Der Handel kann auf bestimmte Wochentage beschränkt werden. Außerhalb der zulässigen Tage werden alle ausstehenden Aufträge entfernt, bestehende Positionen bleiben jedoch unberührt und folgen dem ursprünglichen Verhalten des Beraters.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `Volume Per Trade` | Mit jeder Eingabe übermitteltes Bestellvolumen. |
| `TP Multiplier` | Auf die Stoppdistanz angewendeter Multiplikator zur Berechnung des Take-Profit-Offsets. |
| `ZigZag Depth` | Anzahl der bei der Bestätigung eines Swings untersuchten Kerzen. |
| `ZigZag Deviation` | Mindestabweichung (in Punkten), die erforderlich ist, bevor der ZigZag einen neuen Pivot validiert. |
| `ZigZag Backstep` | Mindestanzahl von Kerzen zwischen gegenüberliegenden ZigZag-Pivots. |
| `Wait Bars After Signal` | Maximale Anzahl an Kerzen, um die ausstehende Bestellung vor der Stornierung am Leben zu halten. |
| `Fast EMA` | Periode des schnellen exponentiellen gleitenden Durchschnitts, der als Trendfilter verwendet wird. |
| `Slow EMA` | Periode des langsamen exponentiellen gleitenden Durchschnitts, der als Trendfilter verwendet wird. |
| `Allow Monday … Allow Friday` | Schaltet um, um den Handel an einzelnen Wochentagen zu aktivieren oder zu deaktivieren. |
| `Candle Type` | Kerzenserien, die für alle Indikatorberechnungen und Handelsentscheidungen verwendet werden. |

## Notizen
- Das Fibonacci-Retracement-Verhältnis ist wie in der Quelle EA auf 57 % festgelegt.
- Stop-Loss- und Take-Profit-Level werden bei Kerzenschließungen überwacht; Intrabar-Spitzen über die Schwellenwerte hinaus lösen bei der nächsten Auswertung Marktaustritte aus.
- Die Strategie behält jeweils eine einzelne ausstehende Bestellung bei und spiegelt die ursprüngliche Implementierung wider.
