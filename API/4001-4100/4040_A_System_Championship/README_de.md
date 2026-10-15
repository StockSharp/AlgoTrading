# Eine Systemmeisterschaftsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 4-Expertenberaters „A System: Championship Strategy Final Edit“ (Datei `ACB6.MQ4`).
- Erkennt bullische oder bärische Ausbrüche in einem konfigurierbaren primären Zeitrahmen und bestätigt die Dynamik mit Live-Geld-/Briefkursen.
- Verwendet einen sekundären Zeitrahmen, um die Trailing-Stop-Distanz zu dimensionieren, und reproduziert die Multithread-Logik des Originals EA über zwei Kerzenströme.
- Implementiert die Blöcke „Global Equity Stop“, „Trade Pause“ und „Adaptive Risk Sizing“, die im Quellroboter fest codiert waren.

## Datenabonnements
- Abonniert zwei Kerzenserien (`PrimaryTimeFrame`, `SecondaryTimeFrame`), um die für Ziele und Trailing Stops verwendeten Preisspannen neu aufzubauen.
- Abonniert Kurse der Stufe 1, um die besten Geld-/Briefkurse zu lesen, die Einstiege, Stop-Loss-Prüfungen, Take-Profits und den Retracement-Ausstieg auslösen.

## Teilnahmebedingungen
1. Warten Sie, bis die primäre Kerze fertig ist, und berechnen Sie deren Spanne multipliziert mit `TakeFactor`.
2. Gehen Sie long, wenn:
   - Die Kerze schließt über ihrem Mittelpunkt.
   - Der aktuelle Briefkurs durchbricht das Kerzenhoch.
   - Der Abstand zwischen dem Gebot und dem Kerzentief beträgt mehr als `MinStopDistance`.
3. Gehen Sie short, wenn die entsprechenden Bedingungen für den Ausbruch nach unten zutreffen.
4. Überspringen Sie Einträge, wenn die berechnete Take-Profit-Distanz kleiner als der minimale Stoppabstand ist.

## Exit-Management
- **Anfängliche Schutzniveaus**: Der Stop ist am vorherigen Kerzentief/-hoch verankert, während der Take-Profit dem Einstiegspreis plus/minus der Spanne multipliziert mit `TakeFactor` entspricht.
- **Retracement-Exit** (`FallLimit`/`FallFactor`):
  - Verfolgen Sie den maximal günstigen Ausflug für die aktive Position.
  - Wenn die aktuelle Bewegung unter `FallLimit * maxMove` fällt *und* die Bewegung bereits über `FallFactor * target` fortgeschritten ist, schließen Sie den Handel zum Marktwert.
- **Trailing Stop** (`TrailFactor`):
  - Die nachlaufende Distanz entspricht dem sekundären Zeitrahmenbereich multipliziert mit `TrailFactor`.
  - Der Stop bewegt sich nur in Handelsrichtung und überschreitet niemals den Take-Profit oder den minimalen Stop-Abstand.
- **Harte Stopps**: Das Berühren des festgelegten Stop- oder Take-Levels durch den Preis führt zu einer sofortigen Liquidation mithilfe von Marktaufträgen.

## Risikomanagement
- **Dynamische Positionsgröße**: kombiniert `RiskPerTrade` mit dem aus `Security.StepSize` und `Security.StepPrice` abgeleiteten Pip-Wert. Das resultierende Volumen wird auf Wechselkursbeschränkungen gerundet und unterschreitet niemals `BaseVolume`.
- **Statistische Anpassung**: Das Verhältnis `LossesExpected/TradesExpected` vom ursprünglichen EA moduliert das Risiko pro Trade, indem es mit der realisierten Verlustquote verglichen wird.
- **Equity Stop** (`SystemStop`): Verfolgt den Aktienhöchststand und deaktiviert neue Trades, wenn der aktuelle Wert unter `SystemStop * peak` fällt. Informationsprotokolle markieren den Stopp der Aktivierung und Wiederherstellung.
- **Handelspause** (`TradePause`): erzwingt ein Abkühlfenster nach jeder Marktorder, genau wie die MT4-Implementierung.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `PrimaryTimeFrame` | 1 Tag | Höherer Zeitrahmen für die Ausbruchserkennung. |
| `SecondaryTimeFrame` | 4 Stunden | Zeitrahmen, der den Trailing-Stop-Bereich bereitstellt. |
| `TakeFactor` | 0,8 | Multiplikator, der bei der Erstellung des Take-Profits auf den primären Kerzenbereich angewendet wird. |
| `TrailFactor` | 10 | Multiplikator, der beim Aktualisieren des Trailing Stop auf den sekundären Kerzenbereich angewendet wird. |
| `FallLimit` | 0,5 | Verhältnis des maximalen Gewinns, der den Retracement-Ausstieg ermöglicht. |
| `FallFactor` | 0,4 | Mindestanteil des Gesamtziels, der erreicht werden muss, bevor ein Retracement-Ausstieg zulässig ist. |
| `RiskPerTrade` | 0,02 | Anteil des jedem Trade zugewiesenen Eigenkapitals vor Anpassungen. |
| `BaseVolume` | 1 | Fallback-Ordergröße, die verwendet wird, wenn die Risikogröße ein kleineres Volumen ergibt. |
| `MinStopDistance` | 0 | Abstandsbeschränkung der Börsenstopps, ausgedrückt in Preiseinheiten. |
| `TradePause` | 5 Minuten | Wartezeit nach jeder ausgeführten Bestellung. |
| `SystemStop` | 0,8 | Drawdown-Faktor für den Portfolio-Aktienstopp (z. B. 0,8 = 20 % zulässiger Drawdown). |
| `LossesExpected` | 20 | Erwartete Anzahl verlorener Trades zur Risikoanpassung. |
| `TradesExpected` | 85 | Erwartete Anzahl der gesamten Trades zur Risikoanpassung. |

## Hinweise zur Implementierung
- Die Sperr-/Absicherungsthreads aus der MQL-Version werden weggelassen, da StockSharp-Strategien auf einer Nettoposition arbeiten. Risikokontrolle und Trailing-Logik bieten einen gleichwertigen Kapitalschutzmechanismus.
- Stop- und Take-Level werden innerhalb der Strategie verfolgt, anstatt separate native Orders zu verwenden, um mit der Backtesting-Engine in Einklang zu bleiben.
- Stellen Sie sicher, dass die verbundene Sicherheit `StepSize`, `StepPrice`, `MinVolume` und `VolumeStep` verfügbar macht. andernfalls fällt die Größenanpassung auf `BaseVolume` zurück.
- Die Strategie sollte mit aktivierten Echtzeitkursen ausgeführt werden. Andernfalls werden nur kerzengesteuerte Aktualisierungen ausgeführt und die Stopplogik reagiert mit Kerzenlatenz.
