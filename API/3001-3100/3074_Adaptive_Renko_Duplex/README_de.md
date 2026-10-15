# Adaptive Renko Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Adaptive Renko Duplex Strategie** ist ein StockSharp-Port des ursprünglichen Experten-Beraters `Exp_AdaptiveRenko_Duplex.mq5`. Die konvertierte Version hält die Idee aufrecht, **zwei unabhängige Adaptive-Renko-Ströme** zu betreiben – einen für bullische Setups und einen für bärische Setups – während die Logik über die High-Level-API bereitgestellt wird. Jeder Strom baut Renko-ähnliche Support- und Resistance-Schienen, deren Steinhöhe sich dynamisch an die jüngste Volatilität anpasst. Die Strategie reagiert auf Trendumkehrungen, die innerhalb dieser Schienen erkannt werden, und kann asymmetrische Konfigurationen für die Long- und Short-Seite beibehalten.

Im Gegensatz zu klassischen Renko-Handelssystemen, die mit synthetischen Steinen arbeiten, hört der Duplex-Ansatz auf Standard-Kerzen und berechnet kontinuierlich die adaptiven Renko-Puffer neu. Signale werden nur bei vollständig abgeschlossenen Kerzen generiert, um Neuzeichnung zu vermeiden und dem ereignisgesteuerten Modell von StockSharp zu entsprechen.

## Marktdaten und Indikatoren
- **Kerzenabonnements** – zwei unabhängige `DataType`-Parameter wählen die Kerzenserien aus, die die Long- und Short-Renko-Ströme speisen. Sie können auf denselben Zeitrahmen oder auf verschiedene zeigen.
- **Adaptive Renko-Rekonstruktion** – jeder Strom enthält die ursprüngliche Indikatorlogik. Eine minimale Steingröße (in Punkten) wird mit `K × Volatilität` verglichen und der größere Wert definiert die neue Steinhöhe. Der Indikator verfolgt obere/untere Envelopes plus farbige Trendniveaus (Unterstützung in Aufwärtstrends, Widerstand in Abwärtstrends).
- **Volatilitätsquellen** – Auswahl zwischen einem `AverageTrueRange`- oder `StandardDeviation`-Indikator. Beide arbeiten auf der Kerzenserie des jeweiligen Stroms und akzeptieren benutzerdefinierte Rückblick-Längen.

## Handelslogik
1. **Long-Seiten-Erkennung**
   - Der Long-Strom baut adaptive Steine mit den konfigurierten Parametern.
   - Wenn die Aufwärtstrendlinie (`RenkoTrend.Up`) auf dem verzögerten Balken erscheint, der durch `LongSignalBarOffset` definiert wird, gibt die Strategie eine Markt-Kauforder aus. Die Ordergröße ist `Volume + |Position|`, was sofortige Umkehrungen von Short zu Long ermöglicht.
   - Wenn eine Abwärtstrendlinie nach der konfigurierten Verzögerung erkannt wird und `LongExitsEnabled` wahr ist, wird das gesamte Long-Engagement geschlossen.
2. **Short-Seiten-Erkennung**
   - Der Short-Strom spiegelt die Logik: Ein `RenkoTrend.Down`-Signal erzeugt einen Markt-Verkauf, während `RenkoTrend.Up` auf dem verzögerten Balken Shorts beendet, wenn `ShortExitsEnabled` aktiviert ist.
3. **Signal-Verzögerung** – beide Seiten respektieren ihre `SignalBarOffset`-Parameter und reproduzieren die Ein-Balken-Verschiebung des MetaTrader-Experten. Das Setzen des Offsets auf null reagiert auf die aktuellste abgeschlossene Kerze.
4. **Positionsgröße** – die StockSharp-Version basiert auf der `Volume`-Eigenschaft der Strategie. Immer vor dem Start der Strategie setzen.

## Risikomanagement
- **Stop-Loss / Take-Profit** – Abstände werden in **Punkten** angegeben und mit dem `PriceStep` des Instruments multipliziert, um absolute Preise zu erzeugen. Stops werden überprüft, wenn eine abonnierte Kerze schließt. Da StockSharp keine serverseitigen Schutzorders automatisch erstellt, werden Ausstiege durch Marktorders abgewickelt.
- **Zustandsverfolgung** – die Strategie speichert den Preis, bei dem der letzte Long- oder Short-Einstieg ausgeführt wurde (basierend auf dem Kerzenschluss), um die Entfernung zum Stop oder Ziel bewerten zu können.
- **Manuelle Überschreibungen** – Standard-`Stop`- oder `Protective`-Module können oben aufgesetzt werden, indem `StartProtection()` extern aufgerufen wird, wenn ein Risikomanagement auf Kontoebene erforderlich ist.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `LongCandleType` | 4-Stunden-Kerzen | Kerzenserie zur Berechnung von Long-Signalen. |
| `ShortCandleType` | 4-Stunden-Kerzen | Kerzenserie zur Berechnung von Short-Signalen. |
| `LongVolatilityMode` | ATR | Volatilitätsquelle (`AverageTrueRange` oder `StandardDeviation`) für Long-Steine. |
| `ShortVolatilityMode` | ATR | Volatilitätsquelle für Short-Steine. |
| `LongVolatilityPeriod` | 10 | Rückblick-Periode für den Long-Volatilitätsindikator. |
| `ShortVolatilityPeriod` | 10 | Rückblick-Periode für den Short-Volatilitätsindikator. |
| `LongSensitivity` | 1.0 | Multiplikator auf den Volatilitätswert vor dem Bau von Long-Steinen. |
| `ShortSensitivity` | 1.0 | Multiplikator auf den Volatilitätswert vor dem Bau von Short-Steinen. |
| `LongPriceMode` | Close | Preiseingabe (`HighLow` oder `Close`) zum Aktualisieren der Long-Renko-Schienen. |
| `ShortPriceMode` | Close | Preiseingabe zum Aktualisieren der Short-Renko-Schienen. |
| `LongMinimumBrickPoints` | 2 | Minimale Steinhöhe für den Long-Strom, in Punkten gemessen. |
| `ShortMinimumBrickPoints` | 2 | Minimale Steinhöhe für den Short-Strom. |
| `LongSignalBarOffset` | 1 | Verzögerung (in Balken) vor der Bestätigung eines Long-Signals. |
| `ShortSignalBarOffset` | 1 | Verzögerung (in Balken) vor der Bestätigung eines Short-Signals. |
| `LongEntriesEnabled` | true | Umschalten, um Long-Einstiege zu erlauben oder zu sperren. |
| `LongExitsEnabled` | true | Umschalten, um Renko-getriebene Long-Ausstiege zu erlauben oder zu sperren. |
| `ShortEntriesEnabled` | true | Umschalten, um Short-Einstiege zu erlauben oder zu sperren. |
| `ShortExitsEnabled` | true | Umschalten, um Renko-getriebene Short-Ausstiege zu erlauben oder zu sperren. |
| `LongStopLossPoints` | 1000 | Stop-Loss-Abstand für Long-Positionen (Punkte × `PriceStep`). |
| `LongTakeProfitPoints` | 2000 | Take-Profit-Abstand für Long-Positionen. |
| `ShortStopLossPoints` | 1000 | Stop-Loss-Abstand für Short-Positionen. |
| `ShortTakeProfitPoints` | 2000 | Take-Profit-Abstand für Short-Positionen. |

> **Punktumrechnung** – die MQL-Version verwendete die Broker-"Punkt"-Definition. In StockSharp wird jeder Abstand mit `Security.PriceStep` (oder `Security.MinStep` als Fallback) multipliziert, um Punkte in absolute Preisinkremente umzurechnen. Passen Sie die Standardwerte an die Tick-Größe Ihres Instruments an.

## Nutzungsrichtlinien
1. **Umgebung konfigurieren** – `Security`, `Portfolio` und `Volume` vor dem Start der Strategie zuweisen. Sicherstellen, dass die Datenquelle alle konfigurierten Kerzen-Zeitrahmen liefern kann.
2. **Beide Ströme anpassen** – Sie können die standardmäßige symmetrische Einrichtung beibehalten oder der Long- und Short-Seite verschiedene Zeitrahmen/Volatilitätsmodi für asymmetrisches Verhalten zuweisen.
3. **Protokolle überwachen** – die Strategie gibt bei jedem Ein- und Ausstieg `LogInfo`-Meldungen aus und gibt das Renko-Niveau an, das die Aktion ausgelöst hat. Diese Protokolle verwenden, um zu überprüfen, ob Signale den Erwartungen entsprechen.
4. **Mit externen Modulen kombinieren** – zusätzliche Filter (Session-Steuerung, Kapitalschutz usw.) können über die High-Level-APIs von StockSharp angehängt werden, da die Strategie die Signale in der Haupt-`Strategy`-Klasse bereitstellt.
5. **Backtesting-Überlegungen** – beim Testen auf historischen Daten bevorzugen Sie Kerzen-Builder, die die erforderlichen Zeitrahmen rekonstruieren können, damit das adaptive Renko konsistent bleibt.

## Unterschiede gegenüber dem ursprünglichen Experten-Berater
- MetaTrader-spezifische Funktionen (Magic Numbers, Geldverwaltungsmodi, Abweichungsbehandlung, Push-Benachrichtigungen) werden absichtlich weggelassen. Die Positionsgröße basiert ausschließlich auf der StockSharp-`Volume`-Eigenschaft.
- Der ursprüngliche EA platzierte serverseitige Stop-Loss- und Take-Profit-Orders. Die konvertierte Version prüft die konfigurierten Abstände bei jeder abgeschlossenen Kerze und schließt über Marktorders.
- Signale werden strikt bei abgeschlossenen Kerzen ausgewertet, um Teilbalken-Neuberechnungen zu vermeiden. Dies spiegelt die `IsNewBar`-Prüfung der MQL-Implementierung wider.
- Die adaptive Renko-Rekonstruktion folgt dem veröffentlichten Algorithmus, ist aber in C# implementiert ohne zusätzliche Indikatorobjekte zu erstellen, was den Aktualisierungspfad effizient hält und die High-Level-API-Konventionen von StockSharp respektiert.

## Empfohlene Erweiterungen
- Den Duplex-Strom mit übergeordneten Regime-Filtern (Session-Zeitpläne, Volatilitätsfilter) kombinieren, um den Handel unter illiquiden Bedingungen zu vermeiden.
- Trailing-Stop-Module oder kapitalbasierte Schutzmechanismen via `StartProtection()` für Safeguards auf Kontoebene anhängen.
- Die generierten Support/Resistance-Schienen protokollieren oder plotten, um die Strategie während der diskretionären Überprüfung visuell zu validieren.
