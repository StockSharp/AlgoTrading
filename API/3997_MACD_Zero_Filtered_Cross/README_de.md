# MACD Null gefiltertes Kreuz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
MACD Zero Filtered Cross ist eine C#-Portierung des MetaTrader 4 Expert Advisors `Robot_MACD_12.26.9`. Die Original-Roboteruhren für
Überschneidungen zwischen der MACD-Linie und ihrer Signallinie, filtert jedoch neue Trades, sodass lange Einträge nur während beider Linien erfolgen
bleiben unterhalb der Nullachse und kurze Einträge treten nur auf, solange beide Linien oberhalb der Achse bleiben. Die StockSharp-Version behält die
Dieselbe Crossover-Logik fügt Risikokontrollen hinzu, die in das Framework integriert sind (Portfolio-Balance-Filterung und einheitliche Gewinnmitnahme).
Management) und legt jeden konfigurierbaren Wert durch Strategieparameter offen, die die Optimierung unterstützen.

Die Strategie setzt auf fertige Kerzen aus einem konfigurierbaren Zeitrahmen. Indikatorwerte werden vom integrierten Gerät bereitgestellt
`MovingAverageConvergenceDivergenceSignal`-Indikator, der sicherstellt, dass die Strategie mit dem übergeordneten API und kompatibel bleibt
respektiert die Nutzungsrichtlinien von `BindEx`.

## Strategielogik
### Indikatorberechnung
* **MACD Linie** – Differenz zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt (Standardlängen: 12 und 26).
* **Signallinie** – exponentieller gleitender Durchschnitt, angewendet auf die Linie MACD (Standardlänge: 9).
* **Nullfilter** – das Vorzeichen beider Linien relativ zu Null bestimmt, ob ein Crossover einen Positionseintrag auslösen kann.

### Einreisebestimmungen
* **Lange Einrichtung**
  * Die Linie MACD muss sich oberhalb der Signallinie (`MACD[t-1] < Signal[t-1]` und `MACD[t] > Signal[t]`) kreuzen.
  * Sowohl die MACD-Linie als auch die Signallinie müssen nach dem Übergang unter Null liegen.
  * Die aktuelle Nettoposition muss flach oder short sein; Bestehende Shorts werden unmittelbar vor dem Versuch einer Long-Position geschlossen.
  * Ein optionaler Balance-Filter erfordert, dass der Portfoliowert einen konfigurierbaren Mindestwert überschreitet, bevor eine neue Order gesendet wird.
* **Kurze Einrichtung**
  * Die Linie MACD muss sich unterhalb der Signallinie (`MACD[t-1] > Signal[t-1]` und `MACD[t] < Signal[t]`) kreuzen.
  * Nach dem Übergang müssen beide Indikatorlinien über Null liegen.
  * Die aktuelle Nettoposition muss flach oder lang sein; Bestehende Long-Positionen werden abgeflacht, bevor eine neue Short-Position gesendet wird.
  * Der Balance-Filter wird symmetrisch auf Short-Einträge angewendet.

### Ausgangsregeln
* **Crossover Exit** – wenn die MACD-Linie die Signallinie gegenüber der aktuellen Position wieder kreuzt, wird die Strategie geschlossen
der offene Handel am Markt. Dies spiegelt das ursprüngliche EA wider, das die Position bei einem gegnerischen Crossover zuvor immer abgeflacht hat
auf der Suche nach neuen Möglichkeiten.
* **Fester Take-Profit** – ein einheitenbasierter Take-Profit (ausgedrückt in Preispunkten) wird über `StartProtection` angewendet. Das Niveau stimmt überein
den MQL-Parameter `TakeProfit` und verwendet den Punktwert des Instruments.

### Risiko- und Kapitalmanagement
* **Volumenhandhabung** – der Parameter `LotVolume` spiegelt die MT4-Losgröße wider. Die Strategie übermittelt für jeden Eintrag genau dieses Volumen.
* **Balance-Filter** – der Parameter `MinimumBalancePerVolume` multipliziert das angeforderte Volumen, um das minimale Portfolio zu bestimmen
Wert erforderlich, bevor neue Einträge zulässig sind. Wenn die Prüfung des Kontostands fehlschlägt, protokolliert die Strategie eine Nachricht und überspringt den Handel.
Entspricht dem ursprünglichen Schutz der freien Marge.
* **Datenintegrität** – Signale werden nur bei fertigen Kerzen verarbeitet und nachdem `IsFormedAndOnlineAndAllowTrading()` dies bestätigt
Sowohl der Anschluss als auch die Anzeigen sind bereit.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `FastPeriod` | EMA Länge der schnellen MACD-Komponente. |
| `SlowPeriod` | EMA Länge der langsamen MACD-Komponente. |
| `SignalPeriod` | EMA Länge der MACD Signalleitung. |
| `TakeProfitPoints` | Abstand zum schützenden Take-Profit in Preispunkten. Zum Deaktivieren auf `0` setzen. |
| `LotVolume` | Basisauftragsvolumen, entspricht der Eingabe „Lots“ der MT4-Version. |
| `MinimumBalancePerVolume` | Erforderlicher Mindestportfoliowert pro gehandelter Volumeneinheit vor der Eröffnung einer Position. Auf `0` setzen, um den Filter zu überspringen. |
| `CandleType` | Zeitrahmen, der zum Aufbau von Kerzen und zur Versorgung der Indikatorkette verwendet wird. |

## Zusätzliche Hinweise
* Die Strategie nutzt die `BindEx`-Überladung, sodass der MACD-Indikator sowohl den MACD- als auch den Signalwert auf einmal liefern kann
Rückruf ohne manuelle Anrufe an `GetValue`.
* Alle Kommentare im C#-Code sind in englischer Sprache verfasst und entsprechen den Projektrichtlinien.
* Für diese Strategie gibt es keine Python-Übersetzung. Im Paket API wird nur die C#-Implementierung bereitgestellt.
* Um das ursprüngliche MT4-Verhalten möglichst genau zu reproduzieren, wählen Sie einen Kerzenzeitrahmen aus, der mit dem Diagramm übereinstimmt, in dem der EA früher ausgeführt wurde
und halten Sie den Volumenparameter im Einklang mit der zuvor gehandelten Losgröße.
