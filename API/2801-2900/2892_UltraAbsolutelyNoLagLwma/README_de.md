# UltraAbsolutelyNoLag LWMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **UltraAbsolutelyNoLag LWMA-Strategie** repliziert die Signale des MetaTrader-Experten Ultra Absolutely No Lag LWMA mithilfe der High-Level-API von StockSharp. Der Indikatorstapel wertet eine doppelte gewichtete gleitende Durchschnittsleiter aus und misst, wie viele Glättungsstufen nach oben oder unten zeigen. Die resultierenden Zählungen werden erneut geglättet, um einen farbcodierten Zustand zu erzeugen, der die Handelslogik steuert. Die Strategie platziert optional Schutz-Stop-Loss- und Take-Profit-Aufträge für jede neue Position.

## Indikator-Pipeline

1. **Doppelter LWMA-Filter** – Der angewandte Kurs (standardmäßig Schlusskurs) wird durch zwei aufeinanderfolgende gewichtete gleitende Durchschnitte verarbeitet, um Rauschen zu entfernen.
2. **Glättungsleiter** – Die gefilterte Reihe durchläuft eine konfigurierbare Menge gleitender Durchschnitte. Jeder Schritt verwendet die ausgewählte Glättungsmethode (standardmäßig Jurik) und eine Länge, die sich um einen festen Schritt erhöht.
3. **Bullen-/Bären-Zähler** – Jeder Schritt vergleicht den aktuellen Wert mit dem vorherigen Wert. Steigende Schritte tragen zum bullischen Zähler bei, fallende Schritte zum bärischen Zähler.
4. **Endglättung** – Die bullischen und bärischen Zähler werden mit der ausgewählten Methode erneut geglättet. Diese beiden Werte bilden den endgültigen Zustand des Indikators.

Die Strategie rekonstruiert die Farblogik des ursprünglichen Indikators: Starke bullische Zustände erzeugen Codes 7–8, neutrale bullische Zustände 5–6, starke bärische Zustände 1–2 und neutrale bärische Zustände 3–4. Null bezeichnet einen undefinierten Zustand.

## Handelslogik

* Wenn der ältere Balken einen bullischen Code (`> 4`) meldete und der neueste Balken zu einem bärischen Code wechselt (`< 5` und ungleich null), schließt die Strategie offene Short-Positionen und kann eine neue Long-Position eröffnen.
* Wenn der ältere Balken einen bärischen Code (`< 5` und ungleich null) meldete und der neueste Balken zu einem bullischen Code wechselt (`> 4`), schließt die Strategie offene Long-Positionen und kann eine neue Short-Position eröffnen.
* Stop-Loss- und Take-Profit-Aufträge können nach jedem Einstieg automatisch registriert werden, wenn die entsprechenden Versätze größer als null sind.

Die Auswertung verwendet die vorherigen zwei abgeschlossenen Balken aus dem Indikatorzeitrahmen, entsprechend dem Verhalten des MetaTrader-Experten, der bei Balkenschluss arbeitet.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `CandleType` | Kerzentyp/-zeitrahmen für die Indikatorberechnungen. |
| `BaseLength` | Länge des doppelten LWMA-Vorfilters. |
| `AppliedPriceMode` | Angewandter Kurs (Schlusskurs, Eröffnungskurs, typisch, DeMark usw.) als Indikatoreingabe. |
| `TrendMethod` | Methode des gleitenden Durchschnitts für die Glättungsleiter (Jurik, SMA, EMA usw.). |
| `StartLength` | Anfangslänge der Glättungsleiter. |
| `StepSize` | Schritt zur Glättungslänge auf jeder Leiterstufe hinzugefügt. |
| `StepsTotal` | Anzahl der Stufen in der Glättungsleiter. |
| `SmoothingMethod` | Methode zur Glättung der Bullen-/Bären-Zähler. |
| `SmoothingLength` | Länge der letzten Glättungsstufe. |
| `UpLevelPercent` | Prozentschwelle für einen stark bullischen Zustand. |
| `DownLevelPercent` | Prozentschwelle für einen stark bärischen Zustand. |
| `SignalBar` | Index des für Handelssignale verwendeten Balkens (1 = vorheriger geschlossener Balken). |
| `AllowBuyOpen` / `AllowSellOpen` | Öffnen von Long-/Short-Positionen aktivieren. |
| `AllowBuyClose` / `AllowSellClose` | Schließen bestehender Long-/Short-Positionen aktivieren. |
| `StopLossOffset` | Absoluter Abstand zwischen Einstiegspreis und dem Schutz-Stop-Loss (0 deaktiviert). |
| `TakeProfitOffset` | Absoluter Abstand zwischen Einstiegspreis und dem Take-Profit (0 deaktiviert). |

## Verwendungshinweise

1. Den Kerzentyp so konfigurieren, dass er dem gewünschten Indikatorzeitrahmen entspricht (die MetaTrader-Version verwendet standardmäßig H4).
2. Leitparameter anpassen, wenn schnellere oder langsamere Reaktionen benötigt werden. Ein größeres `StepsTotal` erzeugt einen glatterem, aber langsameren Indikator.
3. `StopLossOffset` und `TakeProfitOffset` auf null lassen, um Schutzaufträge zu deaktivieren.
4. Die Indikatorzuordnung verwendet StockSharp-gleitende Durchschnitte. Methoden, die in StockSharp nicht verfügbar sind, werden auf Jurik- oder EMA-Glättung zurückgesetzt.
5. Die Strategie handelt nur auf abgeschlossenen Kerzen, um konsistent mit dem ursprünglichen Experten zu bleiben.
