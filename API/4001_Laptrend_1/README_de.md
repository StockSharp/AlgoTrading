# Laptrend_1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Laptrend_1 reproduziert die Logik des Expertenberaters MetaTrader **Laptrend_1.mq4**. Die Strategie kombiniert einen LabTrend-Kanalfilter mit mehreren Zeitrahmen, eine Fisher Transform-Momentumbestätigung und eine ADX-Trendstärkeprüfung für 15-Minuten-Kerzen. Aufträge werden nur dann geöffnet, wenn die LabTrend-Richtungen für den höheren Zeitrahmen (H1) und den Signalzeitrahmen (M15) übereinstimmen, die Fisher-Transformation die Bewegung bestätigt und ADX einen verstärkenden Trend zeigt. Positionen werden geschlossen, wenn sich die Dynamik umkehrt, sich die LabTrend-Richtung ändert oder der Markt in ein flaches Regime übergeht, in dem ADX und die DI-Komponenten konvergieren.

## Handelslogik
- **Primärdaten** – 15-Minuten-Kerzen steuern Ein-/Ausstiege, während 1-Stunden-Kerzen den langfristigen LabTrend-Filter versorgen.
- **LabTrend-Kanal** – Der Code erstellt den Indikator `LabTrend1_v2.1` neu, indem er Kanäle im Donchian-Stil über den letzten `ChannelLength`-Balken erstellt und diese mit dem `RiskFactor` einschränkt. Ein Schlusskurs über dem oberen Band markiert einen Aufwärtstrend; Ein Schlusskurs unterhalb des unteren Bandes markiert einen rückläufigen Trend. Die M15- und H1-Trends müssen sich an offenen Trades orientieren.
- **Fisher-Transformation** – Eine benutzerdefinierte Fisher-Transformation (`Fisher_Yur4ik`) verfolgt die Dynamik im M15-Zeitrahmen. Durch den Nullpunkt wird die bullische/bärische Tendenz umgedreht, während ein Durchschreiten von ±0,25 Ausstiegssignale erzeugt.
- **ADX-Filter** – Der 15-Minuten-Durchschnittsrichtungsindex muss steigen und die dominierende DI-Komponente muss mit dem vorgeschlagenen Handel einverstanden sein. Wenn ADX, +DI und –DI innerhalb von `Delta` Punkten voneinander liegen, behandelt die Strategie den Markt als flach, setzt die Momentum-Flags zurück und liquidiert offene Positionen.
- **Positionsverwaltung** – Neue Positionen schließen jedes gegenteilige Engagement und handeln mit einem konfigurierbaren Volumen. Ausstiege werden durch LabTrend-Umkehrungen, Fisher-Ausstiege oder eine Marktstagnation ausgelöst.

## Risikomanagement
- **Stop-Loss / Take-Profit** – Konfigurierbar in Instrumentenpunkten (MetaTrader „Pips“). Sie werden anhand der Kerzenhochs/-tiefs bewertet, um Schutzaufträge des ursprünglichen EA nachzuahmen.
- **Trailing Stop** – Sobald sich der Preis zu Gunsten des Handels bewegt, verfolgt ein Trailing Stop den Schlusskurs in einem Abstand von `TrailingStopPoints`. Das Überschreiten des Trailing-Levels löst einen sofortigen Marktausstieg aus.
- **Volumen** – Alle Bestellungen verwenden den festen Parameter `Volume` (Lots).

## Parameter
- `Volume` – Bestellgröße in Losen. Standard 1.
- `AdxPeriod` – ADX Glättungszeitraum. Standard 14.
- `FisherLength` – Fenster für die Fisher-Transformation. Standard 10.
- `ChannelLength` – Balken, die für den LabTrend-Kanal verwendet werden. Standard 9.
- `RiskFactor` – LabTrend-Kanalverengungsfaktor (ursprünglicher Indikatorbereich 1..10). Standard 3.
- `Delta` – Maximale Differenz zwischen ADX- und DI-Werten, bevor der Markt als flach gekennzeichnet wird. Standard 7.
- `StopLossPoints` – Stop-Loss-Distanz in Punkten. Standard 100.
- `TakeProfitPoints` – Take-Profit-Distanz in Punkten. Standard 40.
- `TrailingStopPoints` – Trailing-Stop-Distanz in Punkten. Standard 100.
- `SignalCandleType` – Kerzenserie für Signalberechnungen (Standard M15).
- `TrendCandleType` – Kerzenserie für den LabTrend-Filter mit höherem Zeitrahmen (Standard H1).

## Notizen
- Die ursprüngliche MQL-Implementierung funktionierte bei jedem eingehenden Tick; Dieser Port verarbeitet abgeschlossene M15-Kerzen, wodurch die Logik deterministisch bleibt und gleichzeitig die Indikatorberechnungen berücksichtigt werden.
- Stop-Loss, Take-Profit und Trailing-Exits werden mit Marktaufträgen ausgeführt, wenn das Hoch/Tief der Kerze die konfigurierten Schwellenwerte überschreitet. Dies spiegelt das Verhalten von MetaTrader Schutzaufträgen wider, ohne dass explizite Stop-/Limit-Aufträge beibehalten werden.
- Stellen Sie sicher, dass die Datenquelle sowohl die in den Parametern definierte 15-Minuten- als auch die 1-Stunden-Kerzenreihe bereitstellt, bevor Sie mit der Strategie beginnen.
