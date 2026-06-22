# UmnickTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptives Mean-Reversion-System, konvertiert aus dem ursprünglichen UmnickTrader MQL5-Expertenberater. Die Strategie arbeitet immer mit einer einzigen Position, wechselt je nach Ergebnis des vorherigen Trades zwischen Long- und Short-Bias. Sie bewertet Preisbewegungen anhand des Durchschnitts aus Eröffnungs-, Hoch-, Tief- und Schlusskursen und handelt erst, wenn sich dieser Durchschnitt mindestens um die konfigurierte `StopBase`-Distanz verschoben hat.

## Kernlogik

- Für jede abgeschlossene Kerze wird der Durchschnittspreis `(O + H + L + C) / 4` berechnet.
- Signale werden nur verarbeitet, wenn der absolute Unterschied zwischen dem aktuellen Durchschnitt und dem zuletzt verarbeiteten Durchschnitt größer oder gleich `StopBase` ist. Dies imitiert das ursprüngliche EA-Verhalten, auf eine ausreichend große Bewegung zu warten.
- Wenn keine Position offen ist, berechnet die Strategie adaptive Take-Profit- und Stop-Loss-Abstände anhand von zwei Ringpuffern, die die acht jüngsten Gewinn- und Verlustausschläge speichern.
- Nach einem profitablen Trade wird der maximale positive Ausschlag, der während der offenen Position beobachtet wurde, in den Gewinnpuffer gespeichert (minus Spread-Polster), während der Verlustpuffer `StopBase + 7 * Spread` erhält.
- Nach einem Verlust-Trade wird der Gewinnpuffer auf `StopBase - 3 * Spread` zurückgesetzt, der Verlustpuffer mit dem aufgezeichneten Drawdown plus Spread-Polster aktualisiert, und die Handelsrichtung wird für das nächste Setup umgekehrt.

## Trade-Management

- Der Standardabstand für Take-Profit und Stop-Loss beträgt `StopBase`. Wenn der akkumulierte Gewinn- oder Verlustpuffer `StopBase / 2` übersteigt, ersetzen ihre jeweiligen Durchschnittswerte den Standardabstand, um die Ausstiegsniveaus adaptiv zu erweitern oder zu verengen.
- Für Einstiege werden Marktorders verwendet. Die erwarteten Take-Profit- und Stop-Loss-Preise werden von der Strategie selbst gespeichert und verwaltet, sodass Positionen geschlossen werden, wenn Kerzenhochs oder -tiefs die entsprechenden Niveaus berühren.
- Während eine Position aktiv ist, werden die günstigste Bewegung und der tiefste Drawdown anhand von Intrabar-Extremen verfolgt. Diese Statistiken fließen in die Puffer ein, wenn der Trade schließt.
- Es kann immer nur eine Position existieren. Ein neues Signal wird ignoriert, wenn der vorherige Trade noch nicht abgeschlossen ist.

## Parameter

- `StopBase` – Basisabstand (in Preiseinheiten), der erforderlich ist, um eine Bewegung als signifikant zu behandeln und den Standard-TP/SL-Abstand. Standard: `0.017`.
- `TradeVolume` – Volumen für Marktorders. Standard: `0.1`.
- `Spread` – Spread-Kompensation bei der Aktualisierung der adaptiven Puffer. Standard: `0.0005`.
- `CandleType` – Kerzenabonnement zur Auswertung von Durchschnittswerten. Standard: `TimeSpan.FromMinutes(5).TimeFrame()`.

## Klassifikation und Filter

- **Richtung**: Beide (aber nie gleichzeitig).
- **Stil**: Adaptiver Swing / konträrer Trend.
- **Indikatoren**: Preisdurchschnitt, benutzerdefinierte Ausschlagpuffer.
- **Stops**: Dynamischer Stop-Loss und Take-Profit, von der Strategie verwaltet.
- **Komplexität**: Mittel – kombiniert zustandsbehaftete Puffer mit adaptiver Ausstiegsgrößenbestimmung.
- **Zeitrahmen**: Konfigurierbar über `CandleType`.
- **Saisonalität / Nachrichtenfilter**: Nicht verwendet.
- **Risikomanagement**: Positionsgröße wird durch `TradeVolume` festgelegt; Ausstiegsabstände passen sich der jüngsten Performance an.
