# RSI-Levels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **RSI-Levels-Strategie** ist ein direkter Port des MetaTrader-5-Expert-Advisors "RSI Levels". Das System beobachtet ein einzelnes Symbol im gewählten Zeitrahmen und handelt, wenn der Relative Strength Index (RSI) konfigurierbare Überkauft- und Überverkauft-Schwellen kreuzt. Die Strategie nimmt an, dass der Markt zur Mitte zurückkehrt, nachdem der RSI in eine Extremzone gelangt. Fällt der Indikator unter das überverkaufte Niveau, wird eine Long-Position eröffnet; steigt er über das überkaufte Niveau, wird eine Short-Position eröffnet. Es wird nur eine Position gleichzeitig gehalten, und jede Gegenposition wird vor einem neuen Einstieg geschlossen.

## Handelslogik

1. **RSI-Berechnung:** Der RSI wird auf dem Arbeitszeitrahmen mit konfigurierbarer Periode berechnet. Die aktuelle Bar muss abgeschlossen sein, bevor Signale bewertet werden.
2. **Long-Einstieg:** Wird ausgelöst, wenn der aktuelle RSI unter dem überverkauften Niveau schließt, während der vorherige RSI-Wert darüber lag. Besteht eine Short-Position, wird sie sofort geschlossen; andernfalls wird ein neuer Long-Trade mit risikobasierter Positionsgröße eröffnet.
3. **Short-Einstieg:** Wird ausgelöst, wenn der aktuelle RSI über dem überkauften Niveau schließt, während der vorherige RSI-Wert darunter lag. Jede bestehende Long-Exposure wird zuerst geschlossen, danach wird ein neuer Short-Trade erstellt.
4. **Stop Loss:** Ein fester Stop wird in konfigurierbarer Distanz in Symbolpunkten vom Einstiegspreis platziert. Ist der Stop auf null gesetzt, ist er deaktiviert.
5. **Take Profit:** Ein fester Take-Profit wird in konfigurierbarer Distanz in Symbolpunkten vom Einstiegspreis platziert. Ist der Take-Profit null, ist er deaktiviert.
6. **Positionsverwaltung:** Nur eine Position kann gleichzeitig offen sein. Nach dem Schließen einer Position wird der interne Zustand zurückgesetzt, damit das nächste Signal sauber beginnt.

## Positionsgröße

Die Positionsgröße wird aus dem konfigurierten *Risk % per Trade* berechnet. Der Algorithmus multipliziert die Portfolio-Equity mit dem Risikoprozentsatz und teilt das Risikokapital dann durch den Geldwert der Stop-Distanz (Stop-Punkte x Step-Preis). Das resultierende Volumen wird auf den nächstniedrigeren handelbaren Lotschritt gerundet und durch Mindest-/Maximalvolumen der Security begrenzt. Fehlen notwendige Marktdaten (Price Step oder Step Price), protokolliert die Strategie eine Warnung und fällt auf das minimal verfügbare Volumen zurück.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 1-Stunden-Zeitrahmen | Zeitrahmen für Kerzenabonnement und RSI-Berechnung. |
| `RsiPeriod` | 14 | Anzahl Perioden für den RSI-Indikator. |
| `OverboughtLevel` | 70 | RSI-Schwelle, die die überkaufte Zone definiert. |
| `OversoldLevel` | 30 | RSI-Schwelle, die die überverkaufte Zone definiert. |
| `RiskPercent` | 2 | Prozentsatz der Portfolio-Equity, der pro Trade riskiert wird. |
| `StopLossPoints` | 500 | Stop-Loss-Distanz in Symbolpunkten. Null deaktiviert. |
| `TakeProfitPoints` | 1000 | Take-Profit-Distanz in Symbolpunkten. Null deaktiviert. |

## Praktische Hinweise

- Die Strategie benötigt `PriceStep`, `StepPrice`, `MinVolume` und `VolumeStep` auf der Security für genaue Risikogrößen. Fehlen Werte, werden konservative Standards verwendet und Warnungen protokolliert.
- Die Logik nutzt `SubscribeCandles` und `Bind`, um Indikatorwerte ohne manuelles Datenziehen zu erhalten, entsprechend den High-Level-API-Richtlinien.
- Stops und Ziele werden auf Kerzendaten bewertet; Slippage und Gaps können Ausführungen abseits des vorgesehenen Preises verursachen.
- Da das System nur auf abgeschlossene Kerzen reagiert, eignet es sich für Zeitrahmen wie M15, H1 oder H4. Niedrigere Zeitrahmen können zusätzliche Filter zur Rauschreduktion benötigen.

## Verwendung

1. Binden Sie die Strategie an die gewünschte Security und das Portfolio.
2. Passen Sie RSI-Schwellen und Risikokontrollen an die Volatilität des Instruments an.
3. Starten Sie die Strategie und überwachen Sie das Log auf Warnungen zu fehlenden Symbolinformationen.
4. Prüfen Sie Handelsergebnisse und verfeinern Sie Stop-/Take-Profit-Distanzen oder RSI-Niveaus entsprechend der Performance.

Diese StockSharp-Implementierung spiegelt das ursprüngliche MetaTrader-Verhalten wider und stellt Konfiguration sowie Risikomanagement über Standard-Strategieparameter bereit.
