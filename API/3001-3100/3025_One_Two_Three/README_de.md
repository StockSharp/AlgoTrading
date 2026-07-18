# One Two Three Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die One Two Three Strategie handelt Ausbrüche des Chaikin-Oszillators nach einer ausgedehnten Phase flacher Akkumulation. Sie emuliert den originalen MetaTrader 5-Experten, indem sie eine Akkumulations-/Distributions-Linie mit zwei EMAs kombiniert, validiert, dass der Marktdruck mehrere Bars lang neutral geblieben ist, und dann bei einem starken Anstieg des Chaikin-Momentums einsteigt. Der StockSharp-Port hält Lot-Sizing, Stop-Management und Trailing-Logik durch Strategieparameter konfigurierbar.

## Konzept

- Den Chaikin-Oszillator als Differenz zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt berechnen, der auf die Akkumulations-/Distributions-Linie aus den eingehenden Kerzen angewendet wird.
- Die letzten **BarsCount** Oszillatorwerte verfolgen und Bars klassifizieren, bei denen der absolute Chaikin-Wert innerhalb von **FlatLevel** bleibt.
- Handel nur erlauben, wenn mehr als **FlatPercent** Prozent dieser gespeicherten Werte innerhalb des Flat-Bereichs geblieben sind, was auf eine ruhige Akkumulation hinweist.
- Wenn eine neue Kerze abschließt, in die Richtung des Chaikin-Impulses einsteigen, wenn seine Größe **OpenLevel** überschreitet.

## Einstiegsregeln

- **Long**: Der Chaikin-Oszillator auf der gerade geschlossenen Kerze ist größer oder gleich **OpenLevel** und die aktuelle Nettoposition ist nicht positiv.
- **Short**: Der Chaikin-Oszillator auf der gerade geschlossenen Kerze ist kleiner oder gleich dem negativen **OpenLevel** und die aktuelle Nettoposition ist nicht negativ.
- Aufträge werden zum Markt ausgegeben. Wenn die Strategie eine Gegenposition hält, wird die Auftragsgröße erhöht, um das bestehende Exposure zu glätten, bevor der neue Trade eröffnet wird.

## Ausstiegsregeln

- Ein fester Stop-Loss (**StopLossPips**) und Take-Profit (**TakeProfitPips**) werden in Preisabweichungen übersetzt, indem der Sicherheitspreisschritt verwendet wird (1 Pip = 1 Preisschritt), und unmittelbar nach dem Einstieg angewendet.
- Ein optionaler Trailing Stop passt den Schutz-Stop an, sobald der Preis mindestens **TrailingStopPips + TrailingStepPips** zugunsten des Trades läuft. Der neue Stop wird genau **TrailingStopPips** vom aktuellen Schlusskurs entfernt platziert, während der Step-Buffer ein vorzeitiges Anziehen verhindert.
- Wenn innerhalb des abgeschlossenen Kerzenbereichs entweder der Stop oder das Ziel berührt wird, wird die Position zum Markt geschlossen.

## Risiko- und Geldmanagement

- **OrderVolume** kontrolliert die Menge, die mit jeder Marktorder gesendet wird. Die Strategie addiert oder subtrahiert beim Richtungswechsel automatisch die aktuelle Positionsgröße, sodass Umkehrungen in einem einzigen Trade erfolgen.
- Das Setzen eines pip-basierten Parameters auf null deaktiviert diese Komponente (zum Beispiel hält ein Take-Profit von null Trades offen, bis der Stop oder das Gegensignal auftritt).

## Parameter

- **OrderVolume** – Basis-Volumen für Einstiege.
- **StopLossPips** – Abstand in Pips zwischen Einstiegspreis und Schutz-Stop.
- **TakeProfitPips** – Abstand in Pips zwischen Einstiegspreis und Gewinnziel.
- **TrailingStopPips** – Abstand in Pips, der zwischen Preis und Trailing Stop gehalten wird. Auf null setzen, um Trailing zu deaktivieren.
- **TrailingStepPips** – Minimaler Pip-Gewinn über den Trailing-Abstand hinaus, bevor der Stop erneut bewegt wird.
- **FastLength** – Periode des schnellen EMA im Chaikin-Oszillator.
- **SlowLength** – Periode des langsamen EMA im Chaikin-Oszillator.
- **FlatLevel** – Absoluter Chaikin-Wert, der noch als flaches Marktverhalten zählt.
- **OpenLevel** – Chaikin-Größe, die erforderlich ist, um einen neuen Trade auszulösen, sobald die Flat-Bedingung erfüllt ist.
- **BarsCount** – Anzahl der jüngsten Chaikin-Werte, die bei der Berechnung des Flat-Verhältnisses ausgewertet werden.
- **FlatPercent** – Mindestprozentsatz der gespeicherten Werte, die innerhalb des Flat-Bereichs bleiben müssen, um den Handel zu erlauben.
- **CandleType** – Kerzendatentyp oder Zeitrahmen, der die Indikatorberechnungen speist.

## Hinweise

- Die Trailing-Logik spiegelt den MetaTrader-Experten wider: Wenn **TrailingStopPips** ungleich null ist, halten Sie **TrailingStepPips** positiv, um einen stagnierenden Stop zu vermeiden.
- Da StockSharp-Strategien mit dem Sicherheitspreisschritt arbeiten, gehen die pip-basierten Abstände davon aus, dass ein Pip einem Preisschritt entspricht; passen Sie die Parameterwerte entsprechend für Instrumente mit unterschiedlichen Tick-Größen an.
- Die Strategie verarbeitet nur abgeschlossene Kerzen und versucht nicht, intrabar zu reagieren, was dem ursprünglichen Experten entspricht, der bei neuen Bar-Eröffnungen ausführt.
