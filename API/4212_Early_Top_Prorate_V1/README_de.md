# Frühe Top Prorate V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Dokument beschreibt den StockSharp-Port des MetaTrader Expert Advisors **earlyTopProrate_V1**. Die Strategie sucht nach Intraday-Bewegungen, die über die tägliche Eröffnung hinausgehen, und skaliert anhand von drei Gewinnzielen aus der Position heraus. Es wurde unter Verwendung des High-Levels StockSharp API konvertiert, wobei die ursprünglichen Geldmanagement- und Handelsmanagement-Ideen beibehalten wurden.

## Kernlogik

1. **Täglicher Kontext** – die Strategie rekonstruiert den Eröffnungs-, Höchst- und Tiefststand des aktuellen Tages aus den verarbeiteten Kerzen. Die dominante Richtung wird durch den Vergleich von `high - open` und `open - low` definiert.
2. **Eintrittsfenster** – neue Geschäfte können nur zwischen `StartHour` (einschließlich) und `EndHour` (ausschließlich) eröffnet werden. Die Standardkonfiguration handelt die frühe europäische Sitzung.
3. **Eintrittsbedingungen** –
   - Wenn die vorherrschende Richtung bullisch ist und der letzte Schlusskurs über dem Tageseröffnungskurs liegt, eröffnet die Strategie eine Long-Position.
   - Wenn die vorherrschende Richtung bärisch ist und der letzte Schlusskurs unter dem Tageseröffnungskurs liegt, eröffnet die Strategie eine Short-Position.
   - Es ist jeweils nur eine Marktposition zulässig (standardmäßig `MaxPositions = 1`).
4. **Geldverwaltung** – das Volumen jedes Eintrags ergibt sich aus dem ausgewählten Geldverwaltungsmodus (siehe unten). Der Wert wird unter Verwendung des Instrumentenvolumenschritts gerundet und zwischen dem minimalen und maximalen Börsenvolumen eingeklemmt.
5. **Positionshandhabung** – Nach der Eingabe einer Position wendet die Strategie die im nächsten Abschnitt aufgeführten mehrschichtigen Ausstiegsregeln an. Die Regeln spiegeln den ursprünglichen Expert Advisor wider, werden jedoch mit hochrangigen StockSharp-Orders anstelle direkter Stop-Loss-/Take-Profit-Modifikationen implementiert.
6. **Sitzungsschluss** – wenn eine Position offen bleibt, wenn `ClosingHour` erreicht ist, erzwingt die Strategie einen Ausstieg zum Markt.

## Details zum Handelsmanagement

Der ursprüngliche MQL-Expertenberater basiert auf manuellen Stop- und Take-Profit-Anpassungen. Der StockSharp-Port reproduziert das Verhalten mit expliziten Prüfungen bei jeder fertigen Kerze:

- **Break-Even-Rettung** (`BreakEvenTrigger`) – Wenn sich der Preis um die konfigurierte Anzahl von Punkten gegen den Einstiegspreis bewegt, wartet die Strategie auf eine Erholung zurück zum Einstiegspreis und endet dann beim Break-Even.
- **Notstopp** (`StopLoss0`) – wenn die nachteilige Abweichung diesen Abstand überschreitet, wird die Position sofort geschlossen.
- **Stopp zum Einstieg** (`StopLoss1`) – nach einer positiven Bewegung um die angegebene Distanz wird der Schutzstopp auf den Einstiegspreis verschoben.
- **Stopp im Gewinn** (`StopLoss2`) – sobald der Gewinn diese Schwelle erreicht, wird der Schutzstopp über (Long) oder unter (Short) den Einstieg verschoben. Der Offset entspricht `StopLoss2 - StopLoss1` und reproduziert die `setSL2-35`-Logik von MetaTrader.
- **Skalierung** (`TakeProfit1/2/3` und `Ratio1/2/3`) – drei Gewinnziele lösen Teilschließungen des verbleibenden Positionsvolumens aus. Verhältnisse stellen Prozentsätze der aktuellen Position dar, sodass nachfolgende Ziele mit der reduzierten Exposition arbeiten. Das dritte Ziel schließt den gesamten Rest ab.

Alle entfernungsbasierten Parameter arbeiten in *Punkten*. Der Hilfsparameter `PointMultiplier` multipliziert das Instrument `PriceStep`, um die `value * 10 * Point`-Arithmetik aus dem Originalskript zu reproduzieren (Standardmultiplikator = 10).

## Geldverwaltungsmodi

Der Parameter `MoneyManagementType` wählt eines von vier Größenmodellen aus:

| Modus | Beschreibung |
| --- | --- |
| `0` or `1` | Feste Losgröße gleich `BaseVolume` (spiegelt das Verhalten von MQL wider, bei dem die Modi 0 und 1 identisch sind). |
| `2` | Quadratwurzelmodell – verwendet `0.1 * sqrt(balance / 1000) * MoneyManagementFactor`. Sofern verfügbar, wird der aktuelle Portfoliowert verwendet. |
| `3` | Aktienrisikomodell – berechnet `equity / price / 1000 * MoneyManagementRiskPercent / 100` und nähert sich der Formel `AccountEquity/Close[0]` aus MetaTrader an. |

Jedes Ergebnis wird unter Verwendung des Instrumentenvolumenschritts und des minimalen/maximalen Austauschvolumens normalisiert.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzenserien, die für Entscheidungen verwendet werden. Standardmäßig werden 5-Minuten-Kerzen verwendet. |
| `StartHour` / `EndHour` | Handelsfenster in Stunden (0–23). |
| `ClosingHour` | Stunde, zu der eine offene Position geschlossen wird. |
| `TimeZoneShift` | Der informative Zeitzonenversatz wird aus Kompatibilitätsgründen beibehalten. |
| `BaseVolume` | Basislosgröße vor Anpassungen des Geldmanagements. |
| `MaxPositions` | Maximale gleichzeitige Positionen (Standard = 1). |
| `TakeProfit1`, `TakeProfit2`, `TakeProfit3` | Entfernungen der drei Gewinnziele in Punkten. |
| `BreakEvenTrigger` | Verlust in Punkten, der den Break-Even-Rettungsausgang aktiviert. |
| `StopLoss0`, `StopLoss1`, `StopLoss2` | Ungünstige/gewinnbringende Schwellenwerte, die die Schutzstopplogik steuern. |
| `Ratio1`, `Ratio2`, `Ratio3` | Prozentsätze der aktuellen Position, die an jedem Ziel geschlossen wurde. |
| `MoneyManagementType` | Geldverwaltungsmodus (0–3). |
| `MoneyManagementFactor` | Multiplikator für das Quadratwurzelmodell. |
| `MoneyManagementRiskPercent` | Risikoprozentsatz für das Aktienmodell. |
| `PointMultiplier` | Multiplikator, der auf die Preisstufe des Instruments angewendet wird, wenn Punkte in tatsächliche Preisversätze umgerechnet werden. |

## Nutzungshinweise

- Wählen Sie einen Kerzentyp, der der am ausgewählten Veranstaltungsort verfügbaren Datengranularität entspricht. Die standardmäßige 5-Minuten-Serie bietet ein Gleichgewicht zwischen Reaktionsfähigkeit und Geräuschfilterung.
- Bei der Umrechnung punktbasierter Entfernungen in reale Preise multipliziert die Strategie `PriceStep * PointMultiplier`. Passen Sie den Multiplikator an, wenn der Broker Punkte anders definiert als in der ursprünglichen MetaTrader-Umgebung.
- Die Break-Even- und Trailing-Logik erfordert fertige Kerzen, daher kann das Intrabar-Verhalten leicht von der Tick-basierten MetaTrader-Ausführung abweichen. Die README-Datei hebt diese Näherung hervor, damit sie beim Testen berücksichtigt werden kann.
- `TimeZoneShift` wird zur Dokumentation aufbewahrt. Die Handelszeiten selbst müssen mit `StartHour`, `EndHour` und `ClosingHour` konfiguriert werden.

## Erste Schritte

1. Fügen Sie die Strategie Ihrem StockSharp-Projekt hinzu oder führen Sie sie in Designer/Runner aus.
2. Konfigurieren Sie die Kerzenserie (`CandleType`) und die Handelszeiten für das Instrument, mit dem Sie handeln möchten.
3. Passen Sie die punktbasierten Schwellenwerte und Verhältnisse entsprechend der Instrumentenvolatilität an.
4. Wählen Sie einen Geldverwaltungsmodus und legen Sie die entsprechenden Parameter fest (`BaseVolume`, `MoneyManagementFactor`, `MoneyManagementRiskPercent`).
5. Führen Sie die Strategie zunächst im Papierhandel durch, um zu überprüfen, ob das Verhalten Ihren Erwartungen entspricht, bevor Sie sie mit Live-Kapital anwenden.
