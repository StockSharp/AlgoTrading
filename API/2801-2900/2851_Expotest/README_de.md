# Expotest-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Expotest-Strategie ist eine direkte StockSharp-Konvertierung des ursprünglichen `Expotest.mq5`-Expertenberaters. Sie handelt ein einzelnes Instrument mit dem Parabolic-SAR-Indikator und einer einfachen, Martingale-inspirierten Geldmanagement-Regel. Die Strategie eröffnet jeweils nur eine Position und verlässt sich auf vordefinierte Stop-Loss- und Take-Profit-Levels für Ausstiege.

## Handelslogik
- **Indikator**: Parabolic SAR, berechnet auf der ausgewählten Kerzenserie. Sowohl der Beschleunigungsfaktor (`SarStep`) als auch die maximale Beschleunigung (`SarMaximum`) sind konfigurierbar.
- **Einstiegsbedingungen**: Wenn keine Position offen ist, prüft die Strategie die letzte abgeschlossene Kerze.
  - Wenn der Parabolic-SAR-Wert unter oder gleich dem Schlusskurs liegt, wird eine Long-Position eröffnet.
  - Wenn der Parabolic-SAR-Wert über oder gleich dem Schlusskurs liegt, wird eine Short-Position eröffnet.
- **Ausstiegsbedingungen**: Stop-Loss- und Take-Profit-Levels werden in einem festen Abstand vom Einstiegspreis platziert, gemessen in Preisschritten. Bei jeder neuen Kerze überwacht die Strategie, ob die Kerzenspanne einen der Level berührt, und schließt die Position entsprechend. Der Ausstiegstyp (Gewinn oder Verlust) wird für zukünftige Positionsgrößenentscheidungen gespeichert.

## Positionsgrößenbestimmung
- **Basisvolumen**: Definiert durch den Parameter `FixedVolume`, wenn er größer als null ist. Andernfalls schätzt die Strategie die Größe aus den Werten `RiskPercent` und `StopLossPoints` unter Verwendung des aktuellen Portfoliokapitals. Wenn keine Methode eine gültige Größe zurückgibt, wird das Standard-`Strategy.Volume` verwendet.
- **Martingale-Schritt**: Nach einem verlorenen Trade wird die nächste Positionsgröße im Vergleich zum Volumen der verlorenen Position verdoppelt. Ein profitabler Ausstieg setzt den Multiplikator zurück und die nächste Order verwendet wieder das Basisvolumen.

## Konfigurierbare Parameter
- `CandleType` – Datentyp für die Kerzen-Aggregation (Zeitrahmen oder anderes Kerzenformat).
- `SarStep` – Anfangsbeschleunigungsfaktor für den Parabolic SAR.
- `SarMaximum` – Maximaler Beschleunigungsfaktor für den Parabolic SAR.
- `StopLossPoints` – Stop-Loss-Abstand vom Einstieg, ausgedrückt in Preisschritten.
- `TakeProfitPoints` – Take-Profit-Abstand vom Einstieg, ausgedrückt in Preisschritten.
- `RiskPercent` – Prozentsatz des Portfoliokapitals, der pro Trade riskiert werden soll, wenn dynamisches Sizing aktiviert ist.
- `FixedVolume` – Explizites Ordervolumen. Auf `0` setzen, um risikobasiertes Sizing zu aktivieren.

## Zusätzliche Hinweise
- Die Strategie verarbeitet nur abgeschlossene Kerzen, um nah an der ursprünglichen tickbasierten MQL-Implementierung zu bleiben und gleichzeitig mit StockSharp-Abonnements kompatibel zu sein.
- Schutz-Levels werden intern verfolgt anstatt durch separate Stop/Limit-Orders, was die Logik transparent und leicht zu backtesten hält.
- Die Python-Implementierung wird gemäß Anforderung absichtlich weggelassen.
