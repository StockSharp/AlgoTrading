# Raymond Cloudy Day-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Raymond Cloudy Day ist eine Ausbruchsverfolgungsstrategie, die die Handelslogik des ursprünglichen Expertenberaters **„Raymond Cloudy Day für EA“** MQL5 rekonstruiert. Der Algorithmus leitet eine Reihe von Referenzniveaus von einer Kerze mit einem höheren Zeitrahmen ab und verwendet sie, um die Wiederaufnahme der Dynamik im Ausführungszeitrahmen zu erkennen. Der StockSharp-Port behält die ursprünglichen Handelsregeln bei und stellt jede Komponente als konfigurierbare Strategieparameter bereit.

## Marktdaten
- **Signalkerzen** – der Zeitrahmen, in dem Trades ausgeführt werden. Die Strategie abonniert diese Reihe für Einstiegssignale und Positionsmanagement.
- **Pivot-Kerzen** – der höhere Zeitrahmen, der zur Berechnung der Raymond-Levels verwendet wird. Standardmäßig ist dies eine tägliche Kerze, die die MQL5-Eingabe `RayMondTimeframe` reproduziert.

Beide Abonnements werden automatisch über `GetWorkingSecurities` registriert, sodass die Strategie die erforderlichen Datenströme anfordert, sobald sie gestartet wird.

## Raymond-Level-Berechnung
Für jede fertige Pivot-Kerze speichert die Strategie die vier Kernebenen, die durch die ursprüngliche EA definiert sind:

\[
\begin{ausgerichtet}
TradeSS &= \frac{Hoch + Tief + Eröffnung + Schluss}{4} \\
PivotRange &= Hoch - Niedrig \\
ETB &= TradeSS + 0,382 \times PivotRange \\
ETS &= TradeSS - 0,382 \times PivotRange \\
TPB1 &= TradeSS + 0,618 \times PivotRange \\
TPS1 &= TradeSS - 0,618 \times PivotRange \\
TPB2 &= TradeSS + PivotRange \\
TPS2 &= TradeSS – PivotRange
\end{aligned}
\]

Die StockSharp-Implementierung verwaltet den aktuellsten Snapshot dieser Werte und protokolliert jede Aktualisierung, sodass der Benutzer überwachen kann, wie sich die Werte im Laufe der Zeit entwickeln.

## Eingabelogik
Sobald die Raymond-Level verfügbar sind, wertet die Strategie jede fertige Signalkerze aus:

1. **Long-Setup** – Wenn das Tief der Kerze unter `TPS1` fällt und der Schlusskurs über das Niveau zurückkehrt, geht die Strategie eine Long-Position ein. Dies spiegelt die EA-Bedingung `Low[1] < TPS1 && Close[1] > TPS1` wider und erfasst die bullische Ablehnung des Niveaus.
2. **Short-Setup** – Wenn die Kerze vollständig über `TPS1` bleibt, aber darunter schließt, eröffnet die Strategie eine Short-Position (entspricht der ursprünglichen, wenn auch asymmetrischen Regel).

Vor der Platzierung einer neuen Order storniert der Algorithmus alle ausstehenden Orders und schließt gegebenenfalls die Gegenposition, sodass nur noch ein Richtungsgeschäft aktiv bleibt.

## Risikomanagement
Raymond Cloudy Day verwendet symmetrische Schutzversätze, gemessen in Ticks:

- **Stop-Loss** – positioniert `ProtectiveOffsetTicks` unter dem Long-Einstieg (oder über dem Short-Einstieg).
- **Take-Profit** – positioniert `ProtectiveOffsetTicks` über dem Long-Einstieg (oder unter dem Short-Einstieg).

Die Offsets werden mit dem `PriceStep` des Instruments multipliziert, um Ticks in absolute Preisabstände umzuwandeln. Jede abgeschlossene Signalkerze löst eine Prüfung aus, die die Position schließt, wenn eines der Schutzniveaus erreicht wird. Wenn die Strategie flach ist, wird der interne Schutzstatus zurückgesetzt, um veraltete Ebenen zu vermeiden.

## Parameter
| Name | Beschreibung | Standard | Notizen |
|------|-------------|---------|-------|
| `TradeVolume` | Bestellvolumen, das für jeden Eintrag verwendet wird. | `1` | Beim Start mit der Eigenschaft `Volume` synchronisiert. |
| `ProtectiveOffsetTicks` | Abstand in Ticks für Stop-Loss und Take-Profit. | `500` | Mit `PriceStep` multipliziert, um absolute Preise zu erhalten. |
| `SignalCandleType` | Kerzentyp, der Handelssignale erzeugt. | `1 hour` Zeitrahmen | Kann auf einen beliebigen `DataType` gesetzt werden, der Kerzen darstellt. |
| `PivotCandleType` | Längerer Zeitrahmen für Raymond-Level-Berechnungen. | `1 day` Zeitrahmen | Entspricht der `RayMondTimeframe`-Eingabe von MQL EA. |

Alle Parameter unterstützen Optimierungsbereiche und beschreibende Metadaten für StockSharp Designer.

## Zusätzliche Hinweise
- Die Strategie erfordert, dass `PriceStep` durch die verbundene Sicherheit definiert wird. Fehlt es, werden Handelseinträge übersprungen und eine Warnung protokolliert.
- Die Diagrammvisualisierung fügt die Ausführungskerzen zusammen mit den ausgeführten Trades hinzu. Bei Bedarf können zusätzliche benutzerdefinierte Zeichnungen hinzugefügt werden.
- Die Implementierung vermeidet die direkte Abfrage von Indikatorwerten und verarbeitet nur fertige Kerzen unter Einhaltung der Projektrichtlinien in `AGENTS.md`.

## Ursprüngliche EA-Besonderheiten erhalten
- Formeln und Multiplikatoren auf Raymond-Ebene (`0.382`, `0.618`, `1.0`).
- Eingabelogik basierend auf dem ersten Verkaufs-Take-Profit (`TPS1`).
- Symmetrische 500-Punkte-Stop-Loss- und Take-Profit-Offsets, konvertiert in Ticks in der StockSharp-Umgebung.

Mit diesen Komponenten verhält sich die StockSharp-Strategie identisch mit der Quelle EA und bietet gleichzeitig umfangreiche Konfiguration und Protokollierung, die für weitere Forschung und Automatisierung geeignet sind.
