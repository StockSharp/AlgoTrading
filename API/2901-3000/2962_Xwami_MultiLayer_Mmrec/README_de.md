# XWAMI Mehrschicht-MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den ursprünglichen **Exp_XWAMI_NN3_MMRec.mq5**-Expert-Advisor nach StockSharp. Drei unabhängige Schichten (A/B/C) führen den XWAMI-Momentum-Indikator auf verschiedenen Zeitrahmen aus und kombinieren ihre Signale innerhalb einer einzigen genetteten Position. Jede Schicht emuliert die entsprechende MagicNumber aus der MetaTrader-Version, einschließlich ihres Money-Management-Zählers und Schutzlevels.

## Handelslogik

* Für jede Schicht wird eine Momentum-Serie als `Preis - Preis[iPeriod]` unter Verwendung des ausgewählten angewendeten Preises berechnet. Die Differenz wird durch vier sequenzielle Glätter (konfigurierbare Methoden und Längen) geleitet, um die "Up"- und "Down"-Linien des XWAMI-Indikators zu erhalten.
* Signale werden beim `SignalBar`-Versatz bewertet. Wenn der vorherige Balken `up > down` hatte, werden Shorts dieser Schicht geschlossen und ein Long-Einstieg ist erlaubt, wenn der neueste Balken `up <= down` zeigt. Wenn der vorherige Balken `up < down` hatte, werden Longs geschlossen und ein Short-Einstieg ist erlaubt, wenn `up >= down`.
* Bevor in eine neue Richtung geöffnet wird, schließt die Strategie alle entgegengesetzten Positionen anderer Schichten, um StockSharps Netting-Modell zu respektieren. Dies spiegelt das Verhalten des Schließens eines entgegengesetzten Magic-Number-Trades im MQL-Code wider.
* Optionale Stop-Loss- und Take-Profit-Levels (in Preispunkten ausgedrückt) werden bei jeder abgeschlossenen Kerze anhand des Hoch/Tief der Kerze überprüft. Bei Auslösung wird ein sofortiger Ausstieg für diese Schicht erzwungen.

## Money-Management-Zähler

Jede Schicht führt eine fortlaufende Historie ihrer jüngsten Trades. Sobald die Anzahl der Verluste im konfigurierten Rückblick den *LossTrigger* erreicht, wechselt die Positionsgröße vom normalen Volumen zum reduzierten ("Small") Volumen. Erfolgreiche Trades oder geringere Verlustzahlen kehren zur normalen Größe zurück. Kauf- und Verkaufsrichtungen führen ihre eigenen Zähler, genau wie im ursprünglichen MMRec-Helfer.

## Parameter

Die Strategie stellt den vollständigen Parametersatz des MQL-Experten bereit:

* `Layer?CandleType` – Kerzentyp (Zeitrahmen) der Schicht (Standardwerte: A=8h, B=4h, C=1h).
* `Layer?Period` – Verzögerung für die Momentum-Serie.
* `Layer?Method1..4`, `Layer?Length1..4`, `Layer?Phase1..4` – Glättungskonfiguration für die vier XWAMI-Stufen.
* `Layer?AppliedPrice` – angewendete Preisformel (Schluss, Eröffnung, gewichtet, Demark usw.).
* `Layer?SignalBar` – Versatz der Signalkerze (0 = aktuell, 1 = letzte geschlossene Kerze, Standard 1).
* `Layer?AllowBuy/SellOpen` und `Layer?AllowBuy/SellClose` – Berechtigungen für Ein- und Ausstiege.
* `Layer?NormalVolume`, `Layer?SmallVolume` – Handelsgröße in Lots (oder Einheiten) für Normal- und Reduziertmodus.
* `Layer?BuyTotalTrigger`, `Layer?BuyLossTrigger`, `Layer?SellTotalTrigger`, `Layer?SellLossTrigger` – MMRec-Zähler, die den Wechsel zum reduzierten Volumen steuern.
* `Layer?StopLossPoints`, `Layer?TakeProfitPoints` – Schutzlevel in Preispunkten (0 deaktiviert das Level).

## Hinweise

* Die StockSharp-Version verwendet eine einzige Netto-Position. Wenn zwei Schichten nicht übereinstimmen, werden entgegengesetzte Positionen vor dem Einstieg in die neue geschlossen, wobei die beabsichtigte Reihenfolge der Signale erhalten bleibt und Hedging vermieden wird.
* Die Tillson T3-Stufe ist direkt in C# implementiert, um Parität mit dem ursprünglichen Glättungsalgorithmus zu halten. Andere Glättungsmodi werden den eingebauten StockSharp-Indikatoren (SMA, EMA, SMMA/RMA, LWMA, Jurik) zugeordnet.
* Da historische Trade-Abfragen zwischen Plattformen unterschiedlich sind, verfolgt die MMRec-Logik abgeschlossene Trades innerhalb der Strategie und reproduziert dieselben Schwellenwerte ohne Scannen der Terminal-Historie.
