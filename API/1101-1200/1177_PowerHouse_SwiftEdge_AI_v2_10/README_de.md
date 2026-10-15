# PowerHouse SwiftEdge AI v2.10 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Pivot-basierte Ausbruchsstrategie mit Momentum-Filter und einfachem Risikomanagement.
Ein Long-Einstieg erfolgt, wenn der Preis das jüngste Pivot-Hoch mit ausreichend Momentum und Mindestabstand vom vorherigen Signal durchbricht.
Der Short-Einstieg wird bei einem Durchbruch des Pivot-Tiefs mit negativem Momentum ausgelöst.
Positionen verwenden Take-Profit und Stop-Loss in Punkten.

## Berechnung und Ausführung

Dies ist ein deterministisches Beispiel; der historische Titel bedeutet kein trainiertes KI-Modell. C# und Python verwenden die folgende ausdrückliche Interpretation der Kurzbeschreibung.

Verwendet werden abgeschlossene Basiskerzen. Ein Pivot-Hoch liegt strikt über den Hochs von jeweils `PivotPeriod` Nachbarkerzen links und rechts; ein Tief strikt unter deren Tiefs. Gleichheit verhindert einen Pivot. Die Bestätigung erfolgt erst nach Abschluss der rechten Kerzen, ohne rückdatierte Signale. Das letzte bestätigte Hoch und Tief werden getrennt gespeichert.

Long verlangt `vorheriger Close <= Pivot-Hoch` und `aktueller Close > Pivot-Hoch`; Short den umgekehrten Durchbruch des Pivot-Tiefs. Momentum ist `aktueller Close - Close vor MomentumPeriod Kerzen`, strikt größer als `MomentumThreshold` für Long oder kleiner als dessen Negativwert für Short. Beide Richtungen verlangen mindestens `MinSignalBars` abgeschlossene Kerzen seit dem letzten akzeptierten Einstieg; null deaktiviert den Abstand. Abgelehnte Signale werden nicht nachgeholt. EMA-Filter und sechsstündige Wartefrist entfallen.

Aus einer Nullposition wird `Volume` eröffnet, eine Gegenposition mit `Volume + abs(Position)` vollständig umgekehrt. Ausstehende Orders sperren neue Orders. Abstände sind absolute Einheiten des Quotierungspreises: Ein Punkt bedeutet eine Preisdifferenz von 1, keinen Tick, `PriceStep`-Multiplikator oder Prozentsatz. Für das Instrument anpassen. Basis ist der tatsächliche Ausführungspreis, bei gleichgerichteten Teilerhöhungen volumengewichtet; Reduzierungen behalten die Basis, Umkehrungen beginnen eine neue.

Lokaler Schutz schließt die gesamte Position per Marktorder, wenn der aktuelle Long-Bid oder Short-Ask Stop oder Take erreicht. Kerzenschlusskurse prüfen ebenfalls den Schutz; Hoch-/Tiefberührungen werden nicht als geordnete Intrabar-Ereignisse ausgelegt. Schutz gilt unabhängig vom Einstiegsabstand und hat auf derselben Kerze Vorrang vor neuen Einstiegen. Bei Kurslücken kann die Ausführung vom Auslöser abweichen.

Standardwerte: `PivotPeriod=2`, `MomentumPeriod=10`, `MomentumThreshold=0`, `MinSignalBars=5`, `StopLossPoints=50`, `TakeProfitPoints=100`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. Perioden und Ausstiegsabstände müssen positiv, Schwelle und Abstand nichtnegativ sein.
