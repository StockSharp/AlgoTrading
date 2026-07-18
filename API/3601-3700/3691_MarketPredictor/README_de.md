# Marktvorhersagestrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Market Predictor-Strategie ist eine umfassende Adaption des ursprünglichen MetaTrader MarketPredictor-Expertenberaters. Die Logik konzentriert sich auf die kontinuierliche Neuschätzung der erwarteten Preisbewegung durch die Kombination einer Monte-Carlo-Prognose mit adaptiven statistischen Parametern, die aus aktuellen Kerzen gewonnen werden. Die Strategie abonniert Kerzen des ausgewählten Zeitrahmens und verarbeitet nur fertige Balken, um vorzeitige Signale zu vermeiden.

## Kernkonzepte
- **Adaptive Mittelwertschätzung:** Die Strategie verwaltet einen dynamischen Durchschnittspreis (`mu`), der anhand eines einfachen gleitenden Durchschnitts aktualisiert wird. Dies spiegelt den Parameteroptimierungsschritt des ursprünglichen Expert Advisors wider.
- **Volatilitätsgesteuerte Amplitude:** Der ATR derselben Kerzenserie steuert den Amplitudenkoeffizienten (`alpha`) und sorgt dafür, dass die Vorhersage auf Volatilitätsspitzen reagiert.
- **Monte-Carlo-Projektion:** Für jede abgeschlossene Kerze führt die Strategie eine konfigurierbare Anzahl zufälliger Simulationen durch, um den erwarteten Preis (`P_t1`) zu schätzen. Die Prognose entspricht dem Durchschnitt der simulierten Preise.
- **Richtungsentscheidung:** Marktaufträge werden gesendet, wenn die Prognose um mehr als den Schwellenwert `sigma` vom letzten Schlusskurs abweicht. Die Positionsrichtung wird erst umgekehrt, nachdem die vorherige Belichtung vollständig geschlossen wurde.

## Handelsregeln
1. Warten Sie, bis die Kerze fertig ist, und bestätigen Sie, dass alle Indikatoren gebildet sind.
2. Aktualisieren Sie `mu` mit dem Wert SMA und `alpha` mit der ATR-basierten Amplitude.
3. Führen Sie Monte-Carlo-Simulationen rund um den letzten Schlusskurs durch.
4. Wenn der durchschnittliche simulierte Preis über `Close + sigma` liegt, geben Sie eine Long-Position mit einer Marktorder ein, wenn keine Position offen ist.
5. Wenn der durchschnittliche simulierte Preis unter `Close - sigma` liegt, geben Sie eine Short-Position mit einer Marktorder ein, wenn keine Position offen ist.
6. Halten Sie die Position, bis das entgegengesetzte Signal erzeugt wird.

## Parameter
- **InitialAlpha** – Standardamplitude, die verwendet wird, bevor ATR verfügbar wird.
- **InitialBeta** – Platzhalterkoeffizient, der aus Kompatibilitätsgründen mit dem ursprünglichen Expert Advisor beibehalten wird (wird nicht direkt in den Berechnungen verwendet).
- **InitialGamma** – Platzhalter-Dämpfungskonstante, die aus Gründen der Dokumentationskonsistenz beibehalten wird (nicht direkt verwendet).
- **Kappa** – Empfindlichkeitsparameter für das zugrunde liegende Sigmoid-Komponentenkonzept. Es wird zu Referenzzwecken und für zukünftige Erweiterungen gespeichert.
- **InitialMu** – Standardmittelpreis bis zur Bildung des gleitenden Durchschnitts.
- **Sigma** – Erforderliche Abweichung zwischen dem prognostizierten Preis und dem letzten Schlusskurs, um Markteintritte auszulösen.
- **MonteCarloSimulations** – Anzahl der Simulationen, die zur Schätzung des nächsten Preises verwendet werden.
- **CandleType** – Zeitrahmen der Kerzenserie.

## Notizen
- Das übergeordnete StockSharp API verwaltet Kerzenabonnements, Indikatorbindung und Marktauftragsausführung.
- Kommentare im Quellcode erläutern jeden Schritt des Prozesses, um die Wartung zu erleichtern.
- Der Python-Port wird wie gewünscht bewusst weggelassen.
