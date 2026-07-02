# Vlado Williams %R Schwellenwertstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Vlado Williams %R Threshold Strategy** ist eine direkte Umsetzung des MetaTrader 4 Expert Advisors `Vlado_www_forex-instruments_info.mq4`. Der ursprüngliche Roboter handelt mit einem einzelnen Williams %R-Oszillator und ändert sein Marktrisiko immer dann, wenn der Indikator ein benutzerdefiniertes Niveau überschreitet. Dieser StockSharp-Port reproduziert das gleiche Verhalten beim Regimewechsel und stellt gleichzeitig jeden einstellbaren Wert als Strategieparameter für die Optimierung und UI-Steuerung bereit.

### Schlüsselkonzepte
- Tauscht die Richtung des Williams %R-Oszillators relativ zu einem Schwellenwert (Standard: `-50`).
- Hält jeweils höchstens eine Marktposition und kehrt sich erst um, nachdem der vorherige Handel geschlossen wurde.
- Optionale risikobasierte Positionsgrößenbestimmung, die die MetaTrader Money-Management-Formel `AccountFreeMargin * MaximumRisk / price` nachahmt.
- Funktioniert mit jedem Kerzenzeitrahmen über den Parameter `CandleType` (standardmäßige 15-Minuten-Balken).

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzenstrom und berechnen Sie einen Williams %R der Länge `WprLength` (Standard 100).
2. Wenn Williams %R **über** `WprLevel` steigt, markiert die Strategie eine bullische Tendenz:
   - Wenn keine Position offen ist und der vorherige Handel nicht lange dauerte, senden Sie eine marktgerechte **Kauforder**.
   - Besteht eine Short-Position, wird diese sofort geschlossen; Neue Long-Positionen werden bei der nächsten Kerze berücksichtigt, nachdem die Position flach ist.
3. Wenn Williams %R **unter** `WprLevel` fällt, ändert sich die Tendenz in eine bärische Richtung:
   - Wenn keine Position offen ist und der vorherige Handel nicht leer war, senden Sie einen Markt-**Verkaufsauftrag.
   - Wenn eine Long-Position besteht, wird diese sofort abgeflacht.
4. Die Positionsgröße wird durch `CalculateOrderVolume` bestimmt:
   - Wenn `UseRiskMoneyManagement` **wahr** ist, schätzt die Strategie das handelbare Volumen aus dem aktuellen Portfoliowert: `Portfolio.CurrentValue × MaximumRiskPercent ÷ 100 ÷ ClosePrice`.
   - Ansonsten wird die Basis `Strategy.Volume` verwendet.
   - Die resultierenden Lose werden auf das Instrument `VolumeStep` ausgerichtet und durch `MinVolume` / `MaxVolume` begrenzt, sofern diese Grenzen verfügbar sind.

Die Strategie vermeidet absichtlich die Eröffnung einer Umkehrposition in derselben Kerze, die den Ausstieg ausgelöst hat, und entspricht dem ursprünglichen EA-Fluss (`CheckForClose` wird vor `CheckForOpen` ausgeführt).

## Konvertierungshinweise
- Die Standardwerte für die Geldverwaltung folgen dem MT4-Skript: `MaximumRiskPercent` beginnt bei `10` und entspricht der ursprünglichen `MaximumRisk = 10`-Konstante, die ungefähr auf ein Mini-Lot pro Trade abzielte.
- Der `shift`-Parameter (Indikatorverschiebung) von MetaTrader ist in der Quelldatei immer Null; deshalb wurde es weggelassen.
- MT4-Farbargumente (z. B. `Red`, `Blue`) haben kein StockSharp-Äquivalent und werden ignoriert.
- Slippage-Eingaben sind nicht erforderlich, da StockSharp Marktaufträge bereits den aktuell besten Preis verwenden.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Zeitrahmen sowohl für die Signalberechnung als auch für die Auftragsauslösung. |
| `WprLength` | `int` | 100 | Lookback-Periode des Williams %R-Oszillators. |
| `WprLevel` | `decimal` | `-50` | Schwelle, die bullische und bärische Regime trennt. |
| `UseRiskMoneyManagement` | `bool` | `false` | Schaltet die risikobasierte Positionsgröße um. |
| `MaximumRiskPercent` | `decimal` | `10` | Prozentsatz des pro Trade eingesetzten Portfolio-Eigenkapitals, wenn das Risikomanagement aktiviert ist. |

> **Tipp:** Kombinieren Sie die Strategie mit `StartProtection()` oder externen Risikokontrollen, wenn Sie eine automatische Stop-Loss-Abwicklung benötigen. Das ursprüngliche EA stützte sich ebenfalls auf manuelle Überwachung und definierte keine harten Stopps.

## Nutzungsrichtlinien
1. Hängen Sie die Strategie an ein Wertpapier an, das genaue `PriceStep`-, `StepPrice`-, `VolumeStep`- und Volumenlimits offenlegt, damit der Positionsgrößen-Helfer Aufträge korrekt normalisieren kann.
2. Stellen Sie `Volume` auf die gewünschte Fallback-Losgröße ein. Es wird immer dann verwendet, wenn kein Portfolio-Eigenkapital verfügbar ist oder `UseRiskMoneyManagement` deaktiviert ist.
3. Optimieren Sie `WprLevel` und `WprLength`, um das System an verschiedene Märkte anzupassen. Enge Schwellenwerte (z. B. `-20` / `-80`) machen die Strategie selektiver, während breite Schwellenwerte (`-50`) dafür sorgen, dass fast immer investiert wird.
4. Die Strategie folgt dem Trend: Unter unterschiedlichen Bedingungen wird sie häufig umkehren. Erwägen Sie bei Bedarf die Kombination mit Filtern wie längerfristigen Trendprüfungen oder Volatilitätsschwellenwerten.

## Unterschiede zur MetaTrader-Version
- Verwendet Kerzenabonnements und Indikatorbindungen aus der StockSharp-Hochebene API; Es gibt keine manuelle Bestellschleife oder Verlaufsscans.
- Die Risikogröße basiert auf `Portfolio.CurrentValue`. Wenn die Kontobewertung fehlt, greift die Logik auf das statische `Volume` zurück und entspricht dem MT4-Verhalten, bei dem `mm=0` eine feste Losgröße erzwingt.
- Alle Kommentare und Parameterbeschreibungen sind aus Gründen der Konsistenz mit den Repository-Richtlinien auf Englisch.

## Validierungs-Checkliste
- ✅ Strategiedatei, kompiliert mit den StockSharp-Strategievorlagenkonventionen (Tabs, dateibezogener Namespace, XML-Inheritdoc).
- ✅ Parameter, die über `Param()` erstellt und gegebenenfalls zur Optimierung markiert wurden.
- ✅ Williams %R-Werte, die über `Bind` verbraucht wurden, ohne direkten `GetValue()`-Zugriff.
