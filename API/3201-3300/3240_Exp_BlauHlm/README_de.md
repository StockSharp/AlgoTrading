# Exp BlauHlm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Exp BlauHlm-Strategie** ist ein StockSharp-Port des MetaTrader 5-Expertenberaters `Exp_BlauHLM.mq5`. Das System basiert auf dem Blau High-Low Momentum (HLM)-Oszillator, der aktuelle Hochs und Tiefs vergleicht, die Differenz mit einer konfigurierbaren XMA-Pipeline glättet und auf drei diskrete Betriebsmodi reagiert:

- **Breakdown** – handelt einen Null-Linien-Durchbruch der Histogrammkomponente.
- **Twist** – sucht nach Momentumdrehungen innerhalb des Histogramms, um Neigungsübergänge zu erfassen.
- **CloudTwist** – arbeitet mit den oberen und unteren Hüllen des Indikators und reagiert auf „Wolken"-Kreuzungen.

Die StockSharp-Implementierung behält dieselben Parameter, Standardwerte und Handelsregeln bei und übersetzt Geldmanagement-Spezifika in die generische `Volume`-Eigenschaft der Basisstrategie.

## Handelslogik

1. Für jede abgeschlossene Kerze des konfigurierten Zeitrahmens berechnet die Strategie den Blau HLM-Oszillator:
   - Die Differenz zwischen dem neuesten Hoch und dem Hoch `XLength - 1` Bars zurück und eine gespiegelte Differenz für Tiefs berechnen.
   - Negative Beiträge auf null begrenzen und subtrahieren, um den rohen HLM-Wert zu erhalten (in Punkten ausgedrückt wenn das Instrument eine Tick-Größe angibt).
   - Die Sequenz durch vier kaskadierte gleitende Durchschnitte mit identischen Methoden aber unabhängigen Längen glätten.
2. Abhängig vom gewählten **Mode**:
   - **Breakdown** öffnet eine Long-Position wenn der ältere Histogrammwert positiv und der neuere nicht positiv ist (Null-Linien-Erholung) und schließt Shorts in der gleichen Situation. Eine symmetrische Regel behandelt Short-Einstiege/Long-Ausstiege wenn das Histogramm von negativ auf nicht negativ wechselt.
   - **Twist** vergleicht die Histogrammneigung über drei historische Punkte. Eine lokale Beschleunigung (mittlerer Wert steigt nach einem Rückgang) löst Long-Logik aus, während eine Verlangsamung (mittlerer Wert fällt nach einem Anstieg) Short-Logik aktiviert.
   - **CloudTwist** überwacht die zwei geglätteten Hüllen. Wenn das ältere obere Band über dem unteren liegt und die neueren Werte sich unter-/übereinander kreuzen, werden Long- oder Short-Signale entsprechend erzeugt.
3. Das Positionsmanagement folgt den Berechtigungen `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` und verwendet `Volume` der Strategie für Markteinstiege. Entgegengesetzte Signale schließen bestehende Positionen bevor eine neue eröffnet wird.

## Parameter

| Name | Typ | Standard | Beschreibung |
| ---- | --- | -------- | ------------ |
| `CandleType` | `DataType` | `H4`-Kerzen | Vom Oszillator verarbeiteter Zeitrahmen. |
| `SmoothingMethod` | `SmoothMethod` | `Exponential` | Gleitender-Durchschnitt-Methode für jede Glättungsstufe (nicht unterstützte Legacy-Modi fallen auf EMA zurück). |
| `XLength` | `int` | `2` | Spanne in Bars für das rohe Hoch/Tief-Momentum. |
| `FirstLength` | `int` | `20` | Periode der ersten Glättungsstufe. |
| `SecondLength` | `int` | `5` | Periode der zweiten Glättungsstufe. |
| `ThirdLength` | `int` | `3` | Periode der dritten Glättungsstufe. |
| `FourthLength` | `int` | `3` | Periode des finalen Signal-Glätters. |
| `Phase` | `int` | `15` | Jurik-Phase-Parameter (begrenzt auf ±100, von Nicht-Jurik-Glättern ignoriert). |
| `SignalBar` | `int` | `1` | Historischer Versatz beim Vergleich von Indikatorwerten. |
| `EntryMode` | `Mode` | `Twist` | Aus dem MQL-Experten kopierte Handelslogik (`Breakdown`, `Twist`, `CloudTwist`). |
| `BuyOpen` / `SellOpen` | `bool` | `true` | Long/Short-Positionen nach einem Signal eröffnen erlauben. |
| `BuyClose` / `SellClose` | `bool` | `true` | Long/Short-Positionen bei entgegengesetztem Signal schließen erlauben. |

## Konvertierungshinweise

- Die MQL-Bibliothek `SmoothAlgorithms.mqh` enthält proprietäre Filter (JJMA, JurX, ParMA, T3, VIDYA, AMA). StockSharp bietet integrierte Alternativen für die gängigsten Varianten, daher werden nicht unterstützte Modi mit dem exponentiellen gleitenden Durchschnitt approximiert, um den Workflow intakt zu halten.
- Geldmanagement-Parameter (`MM`, `MarginMode`, `StopLoss`, `TakeProfit`, `Deviation`) steuern Ordergröße und Ausführung in MetaTrader. In diesem Port definiert die generische `Volume`-Eigenschaft die Positionsgröße und Orders werden immer zum Marktpreis gesendet.
- Das Signal-Timing spiegelt den `SignalBar`-Versatz des ursprünglichen Experten wider: Die Strategie pflegt einen internen Ringpuffer mit Indikatorwerten und vergleicht historische Snapshots, damit Optimierungsergebnisse konsistent bleiben.
- Der Risikoschutz wird an `StartProtection()` delegiert; globale Stop-Loss/Take-Profit-Regeln bei der übergeordneten Strategie oder dem Trading-Connector konfigurieren, falls erforderlich.

## Verwendungstipps

1. Die `Volume`-Eigenschaft vor dem Start der Strategie setzen, um die Anzahl der Lots/Kontrakte pro Trade festzulegen.
2. Für Symbole ohne sinnvollen `PriceStep` arbeitet der Oszillator in rohen Preiseinheiten. Parameter ggf. neu skalieren wenn der Basiswert große Tick-Größen verwendet.
3. Bei Experimenten mit nicht-exponentiellen Glättern beachten, dass sehr kurze Längen kombiniert mit Jurik-Phase-Extremen zu unruhigen Signalen führen können; Perioden verbreitern für mehr Stabilität.
4. Strategie mit portfolioweiten Risikokontrollen oder den integrierten Schutzregeln kombinieren, um das ursprüngliche Stop-Loss/Take-Profit-Verhalten zu emulieren.
