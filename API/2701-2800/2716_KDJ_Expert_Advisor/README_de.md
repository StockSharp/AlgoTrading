# KDJ Experten-Advisor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader 5-Experten «KDJ Expert Advisor» von senlin ge. Sie handelt ein einzelnes Symbol mit Signalen des KDJ-Oszillators, einer Weiterentwicklung des Stochastik-Oszillators, bei dem die %K-Linie zweimal geglättet wird. Die Strategie beobachtet die Differenz zwischen den %K- und %D-Linien (oft als J-Linie bezeichnet), um Momentum-Umkehrungen zu identifizieren, und öffnet immer nur eine Position. Das Trade-Management spiegelt den ursprünglichen Experten wider: Jeder Trade erhält sofort einen festen Stop-Loss und Take-Profit, ausgedrückt in Pips und in Preisabstand über die Instrumenteneinstellungen übersetzt.

Die Implementierung verwendet StockSharp's High-Level-API mit einem Kerzenabonnement und dem integrierten `Stochastic`-Indikator, konfiguriert, um die KDJ-Parameter der MQL5-Version zu entsprechen. Der Code erkennt automatisch 3- oder 5-stellige Forex-Symbole und passt den Pip-Wert entsprechend an.

## Indikatorlogik
Der zugrunde liegende Indikator arbeitet in drei Stufen:

1. **RSV-Berechnung** – Für jede fertige Kerze wird der Raw Stochastic Value über `KDJ Length` Kerzen berechnet:
   \[
   RSV = \frac{Close - LowestLow}{HighestHigh - LowestLow} \times 100
   \]
2. **%K-Glättung** – Berechnung des Durchschnitts der letzten `Smooth %K` RSV-Werte zur Erhaltung der %K-Linie.
3. **%D-Glättung** – Berechnung des Durchschnitts der letzten `Smooth %D` %K-Werte zur Erhaltung der %D-Linie.

Die Strategie analysiert dann `K - D` (in der Originalquelle als *KDC* bezeichnet) und die Steigung von %K, um Umkehrungen zu erkennen.

## Einstiegskriterien
Eine Marktposition wird nur geöffnet, wenn keine bestehende Position für das Symbol vorhanden ist. Signale werden auf abgeschlossenen Kerzen bewertet:

- **Kauf** wenn eine der folgenden Bedingungen erfüllt ist:
  - `K - D` kreuzt über null (von negativ zu positiv); oder
  - `K - D` ist über null und die %K-Linie steigt (`K_current > K_previous`).
- **Verkauf** wenn eine der folgenden Bedingungen erfüllt ist:
  - `K - D` kreuzt unter null (von positiv zu negativ); oder
  - `K - D` ist unter null und die %K-Linie fällt (`K_current < K_previous`).

Dies entspricht der booleschen Struktur des ursprünglichen MQL5-Experten und gewährleistet identisches Trade-Timing.

## Risikomanagement
- Jede ausgeführte Order erhält einen schützenden Stop-Loss und Take-Profit, gemessen in Pips und in Preisabstand über die Tick-Größe des Instruments umgerechnet. Ein Wert von null deaktiviert das entsprechende Schutz-Bein.
- Die Strategie pyramidisiert nicht und mittelt keine Positionen. Sie bleibt flat, bis die aktuelle Position durch die Schutzorders oder manuelle Intervention geschlossen wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| **Candle Type** | Datentyp/Zeitrahmen der Eingabekerzen. | 15-Minuten-Zeitrahmen |
| **KDJ Length** | Anzahl der Kerzen für die RSV-Berechnung. | 30 |
| **Smooth %K** | Anzahl der RSV-Werte zur Glättung der %K-Linie. | 3 |
| **Smooth %D** | Anzahl der %K-Werte zur Glättung der %D-Linie. | 6 |
| **Stop Loss (pips)** | Abstand des schützenden Stop-Loss. Auf 0 setzen zum Deaktivieren. | 25 |
| **Take Profit (pips)** | Abstand des schützenden Take-Profit. Auf 0 setzen zum Deaktivieren. | 45 |
| **Order Volume** | Mit Marktorders gesendete Menge. | 1 |

Alle Parameter unterstützen Optimierungsbereiche, die den Inputs des ursprünglichen Experten entsprechen.

## Verwendungshinweise
1. Konfigurieren Sie das gewünschte Instrument und den Konnektor im Tester oder in der Live-Umgebung.
2. Passen Sie den Kerzentyp an den Chartzeitrahmen an, den Sie von MetaTrader emulieren möchten.
3. Optimieren Sie optional die KDJ-Parameter, Stop-Loss, Take-Profit oder Order-Volumen.
4. Starten Sie die Strategie. Orders werden nur auf vollständig geformten Kerzen generiert.
5. Der Chart zeigt automatisch Kerzen, den KDJ-Indikator und ausgeführte Trades zur visuellen Bestätigung.

## Unterschiede zum Original-EA
- Verwendet StockSharp's `Stochastic`-Indikator mit Glättungsperioden zur Replikation der MQL5-KDJ-Puffer; keine externe Indikatordatei erforderlich.
- Schutzorders werden durch `StartProtection` verwaltet, das Marktausstiege sendet, wenn sie ausgelöst werden.
- Volumen ist ein fester Parameter anstatt des MQL5-`MoneyFixedMargin`-Risikomodells, was die Implementierung prägnant und auf die Signallogik fokussiert hält.
