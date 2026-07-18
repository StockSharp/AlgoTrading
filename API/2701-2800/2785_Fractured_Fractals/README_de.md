# Fractured Fractals Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des klassischen MetaTrader Expert Advisors "Fractured Fractals". Die Strategie verfolgt bestätigte Williams-Fraktale, platziert Stop-Orders an frischen Ausbruchniveaus und zieht einen Schutz-Stop am entgegengesetzten Fraktal nach.

## Details

- **Quelle**: Konvertiert aus `MQL/20127/Fractured Fractals.mq5`.
- **Marktregime**: Ausbruchsfortsetzung auf jedem von StockSharp unterstützten Instrument.
- **Ordertypen**: Verwendet Stop-Orders für Einstiege und schützende Stop-Orders für Ausstiege.
- **Positionsgröße**: Risikobasiert, gesteuert durch `MaximumRiskPercent` und die adaptive Serien-Logik `DecreaseFactor`.
- **Standardparameter**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 10
  - `ExpirationHours` = 1 Stunde
  - `CandleType` = 1-Stunden-Zeitrahmen
- **Kernindikatoren**: Native Fünf-Balken-Williams-Fraktale, on-the-fly berechnet.
- **Strategietyp**: Long/Short-Ausbruch mit dynamischem Stop-Management.

## Strategielogik

### Fraktalsequenz-Verfolgung

- Pflegt Warteschlangen der letzten fünf Kerzenhochs und -tiefs, um den `iFractals`-Puffer in MT5 nachzuahmen.
- Jedes bestätigte Fraktal verschiebt drei rollende Slots: jüngster, mittlerer und alter. Doppelte Werte werden mit dem Instrumentenpreisschritt als Toleranz ignoriert.
- Long-Signale erfordern, dass das neueste Up-Fraktal das mittlere Fraktal übersteigt; Short-Signale erfordern, dass das neueste Down-Fraktal niedriger ist als das vorherige.

### Einstiegsorders und Ablauf

- Wenn keine Long-Position oder ausstehende Buy-Stop-Order existiert, platziert die Strategie einen Buy-Stop am letzten Up-Fraktal mit einem Stop-Loss am letzten Down-Fraktal.
- Symmetrisch platzieren Short-Einstiege einen Sell-Stop am letzten Down-Fraktal mit einem Schutz-Stop am letzten Up-Fraktal.
- Ausstehende Orders erben eine durch `ExpirationHours` definierte Ablaufzeit. Wenn die Kerzenzeit das Ablaufdatum überschreitet oder die Fraktalhierarchie das Setup ungültig macht (neues niedrigeres Up-Fraktal für Longs oder höheres Down-Fraktal für Shorts), wird die Order storniert.
- Der Bot hält das Buch sauber, indem er entgegengesetzte Orders storniert, sobald eine Position eröffnet wird.

### Schützende Trailing-Stops

- Jedes bestätigte entgegengesetzte Fraktal aktualisiert die schützende Stop-Order: Long-Positionen folgen dem neuesten Down-Fraktal, Short-Positionen folgen dem neuesten Up-Fraktal.
- Stops werden nur enger — neue Niveaus müssen über dem bestehenden Order-Preis liegen, bevor ein Ersatz erfolgt.
- Wenn die Position geschlossen wird, werden alle verbleibenden Stop-Orders sofort storniert.

### Risikomanagement und Verlustseriensteuerung

- `CalculateOrderVolume` repliziert die MT5-Risikoberechnung: Risiko pro Einheit = Einstiegspreis minus Stop-Preis (oder umgekehrt für Shorts).
- Das monetäre Zielrisiko entspricht `Portfolio.CurrentValue * MaximumRiskPercent / 100` mit einem Rückfall auf die `Volume`-Eigenschaft, wenn die Portfoliobewertung nicht verfügbar ist.
- Das resultierende Volumen wird durch Losgröße, Volumenschritt, Mindestvolumen und maximale Volumenbeschränkungen normalisiert, die von `Security` bereitgestellt werden.
- Nach einem Verlustgeschäft erhöht sich der Seriezähler; gewinnbringende oder flache Geschäfte setzen den Zähler zurück. Wenn mehr als ein aufeinanderfolgender Verlust auftritt, wird die Größe um `losses / DecreaseFactor` herunterskaliert.

### Handelsergebnis-Verfolgung

- `OnOwnTradeReceived` aggregiert Fills, um zu bestimmen, wann ein Positionszyklus abgeschlossen ist und ob er positiv, negativ oder flach endete.
- Der Seriezähler und der letzte profitable Zeitstempel spiegeln die ursprüngliche Logik wider, was weitere Erweiterungen (z. B. Analysen) ermöglicht, wenn gewünscht.

## Verwendungshinweise

1. Hängen Sie die Strategie an ein beliebiges Instrument/Portfolio-Paar an, passen Sie `CandleType` an die gewünschte Auflösung an und setzen Sie die Risikoparameter entsprechend der Kontogröße.
2. Stellen Sie sicher, dass der Adapter/Broker Stop-Orders unterstützt; andernfalls ersetzen Sie die Schutzorders durch manuelle Ausstiege in `UpdateTrailingStops`.
3. Da die Implementierung nur abgeschlossene Kerzen verarbeitet, lösen intrabar-Spitzen, die kleiner als die Kerzenauflösung sind, keine Orders aus, genau wie in tickbasierten MT5-Tests.
4. Erwägen Sie das Aktivieren der Protokollierung, um Kommentarnachrichten des C#-Ports zu überprüfen, die das Feedback des ursprünglichen Experten widerspiegeln.
