# Adaptive Strategie für Cyberia-Händler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Cyberia Trader Adaptive Strategy** ist eine C#-Portierung des klassischen MetaTrader-Expertenberaters „CyberiaTrader“. Die
Die Strategie baut den ursprünglichen wahrscheinlichkeitsgesteuerten Kern in StockSharp neu auf und erweitert ihn mit optionalen technischen Filtern.
Es analysiert kontinuierlich Preisschwankungen, um die Wahrscheinlichkeit von Umkehrungen zu messen, und bestätigt das Signal dann optional mit EMA,
MACD, CCI, ADX oder fraktale Filter, bevor Sie Bestellungen senden.

## Wahrscheinlichkeits-Engine
Das Herzstück der Strategie ist der von der MQL-Version inspirierte Wahrscheinlichkeitsrechner. Es verwendet einen adaptiven Abtastzeitraum
(`ValuePeriod`) und prüft historische Balken in festgelegten Schritten, um jeden Balken wie folgt zu klassifizieren:

* **Verkaufswahrscheinlichkeit** – bullischer Balken nach einem bärischen Balken (potenzielle schwindende Chance).
* **Kaufwahrscheinlichkeit** – bärischer Balken folgt einem zinsbullischen Balken.
* **Undefinierte Wahrscheinlichkeit** – alle anderen Balkenkonfigurationen.

Für jede Klasse sammelt die Strategie Statistiken zur durchschnittlichen Amplitude, Trefferquote und Erfolgsquote über `ValuePeriod × HistoryMultiplier`.
Proben. Die adaptive Suche durchsucht Zeiträume von `1` bis `MaxPeriod` (Standard 23) und behält den Zeitraum bei, der die höchsten Ergebnisse liefert
Erfolgsquote. Diese Statistiken werden intern wie folgt offengelegt:

* `BuyPossibility`, `SellPossibility`, `UndefinedPossibility` – aktuelle Balkenklassifizierungswerte.
* `BuyPossibilityMid`, `SellPossibilityMid`, ... – laufende Durchschnitte, die vom ursprünglichen Entscheidungsbaum verwendet werden.
* `PossibilityQuality`, `PossibilitySuccessQuality` – Qualitätsverhältnisse, die für die Diagnose und die automatische Periodenauswahl verwendet werden.

Wenn nicht genügend Verlauf verfügbar ist, wartet die Strategie einfach, bis die Wahrscheinlichkeitsmaschine einen gültigen Stichprobensatz meldet.

## Indikatorfilter
Mit dem ursprünglichen EA konnten zusätzliche indikatorbasierte Module aktiviert oder deaktiviert werden. Der Hafen behält die gleiche Idee bei:

* **EMA-Filter** – vergleicht die Steigung eines EMA (`MaPeriod`) zwischen den letzten beiden fertigen Kerzen.
* **MACD-Filter** – prüft die Beziehung zwischen MACD und seiner Signalleitung (`MacdFast`, `MacdSlow`, `MacdSignal`).
* **CCI-Filter** – kennzeichnet Überkauf-/Überverkauft-Regime mit `CciPeriod`- und ±100-Schwellenwerten.
* **ADX-Filter** – prüft +DI- und −DI-Komponenten (`AdxPeriod`), um die dominante Richtung zu bevorzugen.
* **Fraktalfilter** – erkennt den letzten Swing mithilfe eines konfigurierbaren `FractalDepth`-Fensters und blockiert Orders dafür.
* **Umkehrdetektor** – schaltet die Richtungsflaggen um, wenn eine Wahrscheinlichkeitsspitze das `ReversalIndex`-fache ihres Durchschnitts überschreitet.

Jedes Modul kann über Parameter umgeschaltet werden und spiegelt das Verhalten der ursprünglichen booleschen externen Eingänge wider.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (`CandleType`).
2. Erstellen Sie die Wahrscheinlichkeitsstatistik neu und wählen Sie optional den optimalen Abtastzeitraum für jede fertige Kerze neu aus.
3. Wenden Sie die optionalen Indikatorfilter und den Cyberia-Entscheidungsbaum an, um Kauf-/Verkaufsrichtungen zu aktivieren oder zu deaktivieren.
4. Führen Sie Trades aus, wenn eine Kauf- oder Verkaufsentscheidung aktiv ist, und beachten Sie dabei die globalen Schalter `BlockBuy` und `BlockSell`.
5. Wenden Sie optional einen absoluten Stop-Loss- oder Take-Profit-Schutz an, wenn `StopLossPoints` oder `TakeProfitPoints` angegeben sind.
6. Schließen Sie Positionen frühzeitig, wenn die Entscheidung `Unknown` wird und sich die Wahrscheinlichkeitsqualität verschlechtert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe. |
| `AutoSelectPeriod` | Aktiviert die adaptive Suche über `MaxPeriod`, um das beste Stichprobenfenster zu finden. |
| `InitialPeriod` | Fallback-Wahrscheinlichkeitszeitraum, wenn die automatische Auswahl deaktiviert ist. |
| `MaxPeriod` | Maximaler Zeitraum, der bei der adaptiven Suche berücksichtigt wird (Standard 23 wie EA). |
| `HistoryMultiplier` | Anzahl der in der Statistik verwendeten Stichproben pro Zeitraum (Standard 5). |
| `SpreadFilter` | Mindestbewegung (in Preiseinheiten), die erforderlich ist, um eine Wahrscheinlichkeit als „erfolgreich“ zu behandeln. |
| `EnableCyberiaLogic` | Schaltet den ursprünglichen Entscheidungsbaum um, der Wahrscheinlichkeitsdurchschnitte vergleicht. |
| `EnableMa`, `EnableMacd`, `EnableCci`, `EnableAdx`, `EnableFractals`, `EnableReversalDetector` | Aktivieren Sie einzelne Filter. |
| `MaPeriod` | EMA Länge für den gleitenden Durchschnittsfilter. |
| `MacdFast`, `MacdSlow`, `MacdSignal` | MACD-Konfiguration. |
| `CciPeriod` | Länge des Commodity-Channel-Index. |
| `AdxPeriod` | Durchschnittliche Länge des Richtungsindex. |
| `FractalDepth` | Ungerade Anzahl von Kerzen, die analysiert werden, um den letzten fraktalen Schwung zu erkennen. |
| `ReversalIndex` | Vom Umkehrdetektor verwendeter Multiplikator. |
| `BlockBuy`, `BlockSell` | Harte Schalter, die die Eröffnung von Trades in die angegebene Richtung verhindern. |
| `TakeProfitPoints`, `StopLossPoints` | Optionale absolute Take-Profit- und Stop-Loss-Distanzen. |

## Notizen
* Für die adaptive Zeitraumsuche ist ein ausreichender Verlauf erforderlich: `ValuePeriod × HistoryMultiplier + ValuePeriod` Balken.
* Alle Kommentare wurden in Englisch umgeschrieben und die Logik bleibt mit Indikatorbindungen auf dem hohen Niveau StockSharp API.
* Die Wahrscheinlichkeitsmetriken sind interne Felder, werden jedoch durch Protokolle oder durch Erweiterung der Strategie offengelegt, wenn weitere Diagnosen erforderlich sind.
