# Geldautomat 5 Min. Legacy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Cash Machine 5 Min. Legacy ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `CashMachine_5min`. Das System reagiert auf Impulsumkehrungen, die vom DeMarker-Oszillator und dem schnellen Stochastic-Oszillator bei Fünf-Minuten-Kerzen erkannt werden. Sobald eine Position offen ist, verbirgt die Strategie ihre schützenden Stop-Loss- und Take-Profit-Level und gibt sie nur der internen Logik preis, sodass die Stops auf Brokerseite nicht sichtbar sind. Der Gewinn wird schrittweise über drei benutzerdefinierte Meilensteine ​​hinweg geschützt.

## Strategielogik
### Teilnahmebedingungen
* **Lange Einrichtung** – Warten Sie, bis der DeMarker-Wert über den Schwellenwert von 0,30 steigt, während die Stochastic %K-Linie gleichzeitig 20 überschreitet. Beide Bedingungen müssen ihren Zustand von der vorherigen abgeschlossenen Kerze zur aktuellen ändern. Bei einer Flatrate kauft die Strategie zum Marktwert und verwendet dabei das konfigurierte Auftragsvolumen.
* **Kurzes Setup** – Spiegelbild des langen Falls: DeMarker muss durch 0,70 fallen und Stochastic %K muss unter 80 fallen. Das Signal ist nur gültig, wenn sich die vorherige Kerze auf der gegenüberliegenden Seite beider Grenzen befand. Die Strategie verkauft marktorientiert Leerverkäufe, wenn keine Position offen ist.

### Handelsmanagement
* **Versteckte Risikogrenzen** – eine Long-Position wird geschlossen, wenn der Preis um die Distanz `Hidden Stop Loss` fällt oder um die Distanz `Hidden Take Profit` steigt. Shorts verwenden die symmetrischen Bedingungen mit umgekehrten Grenzen. Die Niveaus werden intern überwacht, ohne dass echte Stop-Orders platziert werden.
* **Stufenweiser Trailing-Stop** – drei Take-Profit-Kontrollpunkte (`Target TP1`, `Target TP2`, `Target TP3`) verschärfen den Stop, wenn der Preis steigt. Bei Long-Positionen wird der Stop auf das Kerzenhoch minus `(target − 13)` Pips angehoben, sobald der Preis einen Kontrollpunkt erreicht. Bei Shorts wird der Stop auf das Kerzentief plus `(target + 13)` Pips gesenkt. Jede Stufe wird nur einmal angewendet und niemals gelockert.
* **Trailing-Ausführung** – nachdem mindestens eine Stufe aktiviert wurde, wird die Position durch Berühren des Trailing-Stops per Marktorder geschlossen.

### Unterstützende Mechanik
* Die Strategie schätzt die Pip-Größe automatisch aus der Preisstufe des Wertpapiers und unterstützt sowohl 4/2-stellige als auch 5/3-stellige Forex-Symbole.
* Indikatorberechnungen und -signale werden durch den auswählbaren Kerzentyp gesteuert (standardmäßig Fünf-Minuten-Kerzen). Es werden nur fertige Kerzen verarbeitet.

## Parameter
* **Versteckter Take-Profit** – versteckte Take-Profit-Distanz in Pips (Standard: `60`).
* **Versteckter Stop-Loss** – versteckte Stop-Loss-Distanz in Pips (Standard: `30`).
* **Ziel TP1 / TP2 / TP3** – Gewinnmeilensteine in Pips, die den abgestuften Trailing Stop aktivieren (Standard: `20`, `35`, `50`).
* **Auftragsvolumen** – Market-Order-Volumen, das für Eingaben verwendet wird (Standard: `0.2`).
* **DeMarker-Länge** – Mittelungszeitraum für den DeMarker-Oszillator (Standard: `14`).
* **Stochastic Länge** – Basis-Lookback für den Stochastic-Oszillator (Standard: `5`).
* **Stochastic %K** – Glättungsfaktor für die %K-Linie (Standard: `3`).
* **Stochastic %D** – Glättungsfaktor für die %D-Linie (Standard: `3`).
* **Kerzentyp** – Zeitrahmen, der zur Berechnung von Indikatoren verwendet wird (Standard: Fünf-Minuten-Kerzen).

## Zusätzliche Hinweise
* Die Strategie eröffnet jeweils nur eine Position und wird nicht sofort umgekehrt; Es wartet darauf, dass der aktuelle Handel geschlossen wird, bevor auf ein neues Signal reagiert wird.
* Schutzniveaus werden im Code über Marktausgänge durchgesetzt, sodass keine ausstehenden Stop-Orders im Orderbuch vorhanden sind.
* Das Paket enthält nur die C#-Implementierung; Es wird keine Python-Version bereitgestellt.
