# Fractured Fractals (MT4) Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detaillierte C#-Portierung des klassischen MetaTrader 4 Expert Advisors `MQL/7696/Fractured_fractals.mq4`. Die Strategie wartet auf neu bestätigte
Williams Fraktalniveaus, Warteschlangen-Breakout-Stop-Orders und Trails-Risiko unter Verwendung der vorherigen Fraktalschwünge. Die Positionsgrößenbestimmung folgt dem
Ursprüngliche Risiko-pro-Trade-Logik mit der adaptiven „DecreaseFactor“-Volumenreduzierung nach Drawdowns.

## Einzelheiten

- **Quelle**: Konvertiert von `MQL/7696/Fractured_fractals.mq4`.
- **Marktregime**: Fortsetzung des Ausbruchs, funktioniert bei jedem Instrument, das zuverlässige fraktale Strukturen bildet.
- **Ordertypen**: Verwendet Stop-Orders für Einstiege und schützende Stop-Orders für Ausstiege.
- **Positionsgrößenbestimmung**: Prozentuales Risikomodell, gesteuert durch `MaximumRiskPercent` mit Verluststreak-Dämpfung durch `DecreaseFactor`.
- **Standardparameter**:
  - `MaximumRiskPercent` = 2 %
  - `DecreaseFactor` = 3
  - `CandleType` = 1-stündiger Zeitrahmen
- **Kernindikatoren**: Native Fünf-Balken-Williams-Fraktalerkennung in der Strategie implementiert.
- **Strategietyp**: Symmetrischer Long/Short-Breakout mit fraktalbasierten Trailing-Stops.

## Strategielogik

### Fraktale Erkennung

- Behält ein rollierendes Fenster mit fünf Kerzenhochs und -tiefs bei, um die `iFractals`-Puffer von MetaTrader zu reproduzieren.
- Ein neues Aufwärts-Fraktal wird bestätigt, wenn das mittlere Hoch die beiden umgebenden Hochs auf jeder Seite überschreitet; Ein Down-Fraktal erfordert das
Mitteltief ist der tiefste Wert in der fünftaktigen Sequenz.
- Wenn ein neues Fraktal erscheint, wird es zusammen mit den drei vorherigen Werten gespeichert und spiegelt die Werte `cfu`, `pfu` und von EA wider
Puffer im `pfu.1`-Stil für spätere Vergleiche und nachträgliche Berechnungen.

### Eintragseinrichtung

- Für Long-Trades ist es erforderlich, dass das jüngste Aufwärts-Fraktal das vorherige übersteigt und das jüngste Abwärts-Fraktal eine Risikountergrenze definiert.
Die Strategie platziert dann einen Kaufstopp leicht über dem Fraktal (Spread-Kompensation) und einen Schutzstopp unter dem Gegenwert
Down-Fraktal.
- Short-Trades spiegeln die Logik wider: Ein Fraktal mit einem niedrigeren Tief in Kombination mit einem Fraktal mit einem höheren Aufwärtstrend erzeugt einen Verkaufsstopp und einen Schutz
Stoppen Sie über dem Aufwärts-Fraktal-Plus-Spread.
- Es ist nur eine ausstehende Bestellung pro Richtung zulässig. Wenn die fraktale Struktur das Muster ungültig macht – zum Beispiel die neueste fraktale Nr
länger als die vorherige – die ausstehende Bestellung wird sofort storniert.

### Stoppen Sie die Verwaltung

- Sobald der Bot positioniert ist, verfolgt er den Schutzstopp mithilfe des vorherigen Fraktals auf der Eintrittsseite und subtrahiert/addiert den Strom
verbreiten. Der Stop bewegt sich nur zu Gunsten des Handels.
- Wenn sich die Positionsrichtung ändert oder schließt, wird die ungenutzte Stop-Order gelöscht, um ein veraltetes Exposure zu verhindern.

### Risikomanagement

- `CalculateOrderVolume` repliziert die Risiko-pro-Trade-Berechnung von EA: Die Positionsgröße ist das Verhältnis der monetären Risikotoleranz zu
der Abstand zwischen Einstiegs- und Stoppebene.
- Kontobewertung bevorzugt `Portfolio.CurrentValue`; Wenn die Routine nicht verfügbar ist, greift sie auf die `Volume`-Eigenschaft der Strategie zurück
multipliziert mit dem Preis.
- Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird das Volumen um `losses / DecreaseFactor` reduziert, was dem MetaTrader entspricht.
`DecreaseFactor` Verhalten.

### Verfolgung des Handelszyklus

- `OnOwnTradeReceived` aggregiert Füllungen in Handelszyklen, verfolgt variable PnL und aktualisiert die Verluststrähne, sobald das Volumen zurückkehrt
zu flach. Dadurch bleibt die Risikologik an der des MT4-Experten ausgerichtet, bei dem `HistoryTotal` zur Analyse früherer Ergebnisse verwendet wurde.

## Nutzungshinweise

1. Hängen Sie die Strategie an ein beliebiges Wertpapier-/Portfoliopaar an und wählen Sie eine geeignete `CandleType`-Auflösung, die dem Original entspricht
EA eingerichtet.
2. Stellen Sie sicher, dass Notierungen der Stufe 1 verfügbar sind – die Spread-Schätzung basiert auf dem besten Geld-/Briefkurs; Bei Nichtverfügbarkeit wird auf die Strategie zurückgegriffen
`PriceStep`.
3. Bei den Stop-Orders wird davon ausgegangen, dass der Broker serverseitige Stops unterstützt. Ersetzen Sie die `BuyStop`/`SellStop`-Registrierung durch Marktaufträge, wenn
wird von Ihrem Adapter benötigt.
4. Da die Verarbeitung beim Kerzenschluss erfolgt, werden intrabar-fraktale Signale nur am Ende jedes Balkens bearbeitet und reproduzieren
Bar-für-Bar-Bewertung des Fachberaters.
