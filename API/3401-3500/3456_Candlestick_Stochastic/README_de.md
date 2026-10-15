# Candlestick Stochastic Bestätigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader Expert Advisor **Expert_CP_Stoch** innerhalb des übergeordneten API von StockSharp. Es kombiniert japanische Candlestick-Umkehrmuster mit einem stochastischen Oszillatorfilter %D, um Ein- und Ausstiege zu bestätigen. Das System scannt jede abgeschlossene Kerze, schaut drei Balken zurück, um bullische oder bärische Formationen zu erkennen, und erfordert, dass sich die stochastische Signallinie in einer überverkauften oder überkauften Zone befindet, bevor Trades eröffnet werden. Positionen werden immer dann geschlossen, wenn das entgegengesetzte Muster auftritt oder wenn die stochastische Linie eine konfigurierbare Ausstiegsgrenze kreuzt.

Die Standardkonfiguration spiegelt den ursprünglichen Experten wider: %K-Periode 33, %D-Periode 37, Verlangsamung 30, überverkauft bei 30, überkauft bei 70 und Ausgangs-Crossover-Niveaus bei 20/80. Da der stochastische Oszillator von StockSharp Hoch-/Tief-/Schlussdaten verwendet, entspricht das Verhalten der ursprünglichen STO_LOWHIGH-Einstellung. Die Kerzenmustererkennung stützt sich (standardmäßig) auf die letzten zwölf Körper, um die durchschnittliche Kerzengröße zu berechnen, die bei der Musterfilterung verwendet wird.

## Einzelheiten

- **Eintrittskriterien**:
  - **Long**: Eines der bullischen Muster (Three White Soldiers, Piercing Line, Morning Doji, Bullish Engulfing, Bullish Harami, Morning Star, Bullish Meeting Lines) wird erkannt **und** der stochastische %D-Wert auf dem zuvor geschlossenen Balken liegt unter der überverkauften Schwelle (Standard 30).
  - **Short**: Eines der bärischen Muster (Three Black Crows, Dark Cloud Cover, Evening Doji, Bearish Engulfing, Bearish Harami, Evening Star, Bearish Meeting Lines) wird erkannt **und** der stochastische %D-Wert auf dem zuvor geschlossenen Balken liegt über der überkauften Schwelle (Standard 70).
- **Ausstiegskriterien**:
  - **Long**: Sofort beenden, wenn ein rückläufiges Muster auftritt oder wenn %D die obere Ausstiegsgrenze (Standard 80) oder die untere Grenze (Standard 20) unterschreitet.
  - **Short**: Sofort aussteigen, wenn ein bullisches Muster erscheint oder wenn %D die untere Ausstiegsgrenze (Standard 20) oder die obere Grenze (Standard 80) überschreitet.
- **Long/Short**: Handel in beide Richtungen mit symmetrischen Regeln.
- **Stops**: Kein fester Stop-Loss/Ziel; Ausstiege beruhen auf Musterwechseln und stochastischen Kreuzungen. Bei Bedarf können Sie im Launcher einen Portfolioschutz hinzufügen.
- **Standardwerte**:
  - `Body Average Period` = 12 Kerzen zur Berechnung der Referenzkörpergröße für die Musterqualifizierung.
  - `Stochastic %K` = 33, `Stochastic %D` = 37, `Stochastic Smoothing` = 30.
  - `Oversold Threshold` = 30, `Overbought Threshold` = 70.
  - `Lower Exit Level` = 20, `Upper Exit Level` = 80.
- **Filter**:
  - Kategorie: Mustererkennung + Oszillatorbestätigung.
  - Richtung: Lang und kurz.
  - Indikatoren: Stochastic-Oszillator, mehrere Kerzenmuster.
  - Stopps: Nur Muster-/Oszillatorausgänge (kein mechanischer Stopp/Ziel).
  - Komplexität: Hoch (Mustererkennung mit mehreren Bedingungen und historischen Durchschnittswerten).
  - Zeitrahmen: Funktioniert in jedem Zeitrahmen; Standardmäßig werden stündliche Kerzen verwendet.
  - Saisonalität: Keine.
  - Neuronale Netze: Nein.
  - Divergenz: Keine explizite Divergenz; Bestätigung über Oszillatorpegel.
  - Risikostufe: Mittel-hoch, da keine harten Stopps vorhanden sind.

## Wie es funktioniert

1. Abonniert die ausgewählte Kerzenserie und bindet einen stochastischen Oszillator (%K, %D, Verlangsamung).
2. Behält die letzten drei abgeschlossenen Kerzen und die gleitenden Durchschnitte der Kerzenkörper/-schlüsse bei, um die Musterbibliothekslogik von MetaTrader nachzubilden.
3. Bewertet bullische/bärische Mustergruppen für jede fertige Kerze. Jedes Muster folgt strikt den ursprünglichen mathematischen Definitionen (gemittelte Körperprüfungen, Mittelpunktbeziehungen, Lückenanforderungen usw.).
4. Ruft die stochastischen %D-Werte der beiden vorherigen Kerzen ab, um überverkaufte/überkaufte Bedingungen und Überschneidungen zu erkennen.
5. Öffnet oder schließt Marktpositionen mithilfe der übergeordneten `BuyMarket`/`SellMarket`-Methoden von StockSharp, wenn sowohl das Muster als auch die Oszillatorbedingungen übereinstimmen.
6. Optional können Sie externe Risikomodule (z. B. `StartProtection`) über den Launcher aktivieren, wenn Sie ein Stop-Loss-Management benötigen.

## Praktische Hinweise

- Stellen Sie sicher, dass Sie die Strategie mit mindestens `Body Average Period + 3` historischen Kerzen füttern, bevor Sie Signale erwarten; andernfalls geben Musterprüfungen „false“ zurück, da der durchschnittliche Körper undefiniert ist.
- Der stochastische Filter verwendet den %D-Wert der **vorherigen** Kerze und reproduziert die Art und Weise, wie das Signal von MetaTrader `StochSignal(1)` ausgewertet hat.
- Da die Erkennung von Kerzenmustern empfindlich auf Lücken und relative Kerzengrößen reagiert, können die Ergebnisse bei Instrumenten mit geringer Liquidität oder synthetischen Daten variieren.
- Um die Optimierung zu beschleunigen, können Sie die Schwellenwerte für Überverkauft/Überkauft und die stochastischen Zeiträume feinabstimmen und dabei die Candlestick-Definitionen beibehalten.
- Wenn Sie das Verhalten von STO_CLOSECLOSE (Schließen/Schließen stochastisch) benötigen, ersetzen Sie in einer zukünftigen Erweiterung den Oszillator von StockSharp durch einen, der für Nur-Schließen-Berechnungen konfiguriert ist.
