# Exp XRSI-Histogramm-Vol-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Konvertierung des ursprünglichen MQL5 Expert Advisors `Exp_XRSI_Histogram_Vol`. Sie handelt Ausbrüche im volumengewichteten RSI-Histogramm, indem sie die fünf Farbzustände interpretiert, die der Indikator erzeugt. Das Skript läuft auf jedem Zeitrahmen, der durch das Kerzenabonnement bereitgestellt wird, und basiert auf der High-Level-Strategie-API von StockSharp.

## Strategielogik

1. Einen RSI auf dem ausgewählten Zeitrahmen berechnen und 50 subtrahieren, um den Oszillator zu zentrieren.
2. Den zentrierten RSI-Wert mit dem gewählten Volumenstrom (Ticks oder reales Volumen) multiplizieren, um Kerzen mit starker Aktivität zu betonen.
3. Sowohl den gewichteten RSI als auch das Rohvolumen mit derselben gleitenden Durchschnittsmethode und Länge glätten.
4. Adaptive Schwellenwerte aufbauen, indem das geglättete Volumen mit vier benutzerdefinierten Multiplikatoren multipliziert wird. Das resultierende Histogramm wird in folgende Farbzustände klassifiziert:
   - **0** – starker bullischer Impuls (über `HighLevel2`).
   - **1** – moderater bullischer Impuls (zwischen `HighLevel1` und `HighLevel2`).
   - **2** – neutrale Zone.
   - **3** – moderater bärischer Impuls (zwischen `LowLevel2` und `LowLevel1`).
   - **4** – starker bärischer Impuls (unter `LowLevel2`).
5. Einstiegs- und Ausstiegsregeln spiegeln die MQL-Logik:
   - Den ersten Long eingehen, wenn das Histogramm in den Zustand **1** wechselt, nachdem es über dem Zustand **1** lag (die Farbe sinkt von bärisch/neutral zu moderat bullisch).
   - Den zweiten Long eingehen, wenn das Histogramm in den Zustand **0** wechselt, nachdem es über dem Zustand **0** lag.
   - Den ersten Short eingehen, wenn das Histogramm in den Zustand **3** wechselt, nachdem es unter dem Zustand **3** lag.
   - Den zweiten Short eingehen, wenn das Histogramm in den Zustand **4** wechselt, nachdem es unter dem Zustand **4** lag.
   - Short-Positionen schließen, wenn das Histogramm in den Zuständen **0** oder **1** ist.
   - Long-Positionen schließen, wenn das Histogramm in den Zuständen **3** oder **4** ist.
6. Die Signalgenerierung kann durch `SignalBar` Balken zurückverschoben werden, um die ursprüngliche Indikatorpuffer-Indizierung zu imitieren.

Zwei Skalierungseinstiege werden für jede Richtung durch die Multiplikatoren `Mm1` und `Mm2` unterstützt. Die Hilfsmethoden schließen eine entgegengesetzte Position, bevor eine neue geöffnet wird, und replizieren das Verhalten des Legacy-Trade-Management-Codes.

## Geldmanagement und Schutz

- `Mm1` und `Mm2` sind Volumen-Multiplikatoren, die auf die `Volume`-Eigenschaft der Strategie angewendet werden (ein Standardwert von 1 wird verwendet, wenn `Volume` nicht gesetzt ist).
- Globaler Stop-Loss und Take-Profit werden durch `StartProtection` aktiviert, wenn sowohl der Preisschritt als auch die entsprechenden Punktwerte positiv sind. Sie werden als Anzahl von Preisschritten interpretiert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Zeitrahmen für Kerzen und Indikatorberechnungen. |
| `RsiPeriod` | RSI-Länge. |
| `VolumeMode` | Wahl zwischen Tick-Volumen und realem Volumen. Der Tick-Modus fällt auf eine Einheit zurück, wenn Volumendaten fehlen. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplikatoren, die das geglättete Volumen skalieren, um Histogrammschwellenwerte zu erstellen. |
| `MaMethod`, `MaLength`, `MaPhase` | Glättungseinstellungen. Nicht unterstützte Methoden (Parabolic, T3, Vidya, Ama) fallen auf Simple Moving Average zurück. `MaPhase` wird der Vollständigkeit halber beibehalten, beeinflusst aber nur fortgeschrittene Methoden wie Jurik. |
| `SignalBar` | Wie viele geschlossene Balken zurück beim Lesen der Histogrammfarbe ausgewertet werden sollen. |
| `Mm1`, `Mm2` | Volumen-Multiplikatoren für die erste und zweite Position in jede Richtung. |
| `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` | Öffnungs- und Schließungslogik für Longs/Shorts aktivieren oder deaktivieren. |
| `StopLossPoints`, `TakeProfitPoints` | Schutzabstände in Preisschritten ausgedrückt. |

## Standardwerte

- Kerzentyp: 4-Stunden-Zeitrahmen.
- RSI-Länge: 14.
- Volumenmodus: Tick-Volumen.
- Histogrammschwellenwerte: `HighLevel2 = 17`, `HighLevel1 = 5`, `LowLevel1 = -5`, `LowLevel2 = -17`.
- Gleitender Durchschnitt: SMA mit Länge 12 und Phase 15.
- Signalbalken-Versatz: 1 Balken.
- Geldmanagement: `Mm1 = 0.1`, `Mm2 = 0.2`.
- Stops: Stop-Loss 1000 Punkte, Take-Profit 2000 Punkte (nur angewendet, wenn ein gültiger Preisschritt verfügbar ist).

## Hinweise

- Die Strategie stützt sich auf fertige Kerzen und ignoriert unfertige Updates.
- Jurik-Glättung wird über StockSharp's `JurikMovingAverage` unterstützt. Andere Legacy-Methoden (ParMA, T3, VIDYA, AMA) fallen aufgrund fehlender nativer Entsprechungen auf SMA zurück.
- Der Indikator verwendet das `TotalVolume` der Kerze. Wenn das Volumen null ist, verwendet der Tick-Modus ein Fallback-Gewicht von eins, um die Unterdrückung von Signalen zu vermeiden.
- Für visuelle Analysen wird der RSI selbst neben Kerzen und Trade-Markierungen angezeigt. Sie können zusätzliche Chartelemente anfügen, wenn tiefere Diagnosen erforderlich sind.
