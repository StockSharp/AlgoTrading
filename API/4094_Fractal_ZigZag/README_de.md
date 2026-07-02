# Fraktale Zick-Zack-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des MetaTrader 4 Expertenberaters **Fractal ZigZag Expert.mq4**. Es stellt den Gesetzentwurf wieder her Williams
fraktale Sequenz und interpretiert das jüngste bestätigte Extremum als den aktiven Marktzweig. Wenn das letzte gültige Fraktal a ist
Wenn Sie nach unten schwingen, eröffnet das System eine Long-Position. Wenn ein Swing-Hoch bestätigt wird, wird ein Short eröffnet. Die Implementierung behält die
ursprüngliche Parameter – Fraktaltiefe, Take-Profit, Initial-Stop- und Trailing-Stop-Abstände – bei gleichzeitiger Anpassung des Order-Routings an
das High-Level StockSharp API.

Die Strategie eignet sich am besten für H1-Kerzen und repliziert das in der MetaTrader-Version verwendete Standarddiagramm. Dennoch ist die
Der Parameter `CandleType` ermöglicht den Wechsel zu jedem anderen vom Datenfeed unterstützten Zeitrahmen. Alle Entfernungen sind im Preis angegeben
Punkte (Instrumentenpreisschritte), was die Art und Weise widerspiegelt, wie MetaTrader die `Point`-Konstante verwendet.

## Handelsregeln

- **Signalerkennung**
  - Der Algorithmus scannt jede fertige Kerze und erstellt ein rollierendes Fenster mit `2 * Level + 1`-Elementen.
  - Ein hohes Fraktal wird bestätigt, wenn die mittlere Kerze innerhalb dieses Fensters das höchste Hoch aufweist; Ein niedriges Fraktal erfordert das niedrigste
niedrig.
  - Nur das letzte bestätigte Fraktal bestimmt die Richtung: Ein Tief setzt den internen Trend auf `2` (bullisch), ein Hoch setzt ihn auf
`1` (bärisch).
- **Einträge**
  - Wenn der interne Trend `2` entspricht und keine offene Position vorhanden ist, wird ein Marktkauf mit dem Volumen `Lots` gesendet.
  - Wenn der Trend ohne Position gleich `1` ist, wird ein Marktverkauf gesendet.
  - Die Strategie wird in die gleiche Richtung zurückkehren, nachdem eine Position geschlossen wurde, sofern sich der Trend nicht gewendet hat.
- **Exits & Risikomanagement**
  - Jeder Eintrag erhält einen anfänglichen Stop-Loss und einen festen Take-Profit, der in Punkten definiert ist. Ein Stoppwert von `0` deaktiviert die
entsprechenden Schutz.
  - Der optionale Trailing Stop (auch in Punkten) wird aktiviert, sobald sich der Preis um die konfigurierte Distanz bewegt. Anschließend wird der Anschlag angefahren
Behalten Sie den gleichen Abstand zum Schlusskurs bei und überschreiten Sie niemals den anfänglichen Schutzstopp.
  - Schutzaufträge werden nachgeahmt, indem die Kerzenhochs/-tiefs überwacht werden, um Intrabar-Berührungen anzunähern, die dem Original weitgehend entsprechen
MQL4-Logik.

## Standardparameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Level` | `2` | Anzahl der Kerzen auf jeder Seite, die zur Bestätigung eines Fraktals erforderlich sind. |
| `TakeProfitPoints` | `25` | Abstand zum Take-Profit-Ziel in Preispunkten. |
| `InitialStopPoints` | `20` | Abstand zum anfänglichen Stop-Loss in Preispunkten. |
| `TrailingStopPoints` | `10` | Trailing-Stop-Distanz in Preispunkten (zum Deaktivieren auf `0` setzen). |
| `Lots` | `1` | Für Markteintritte verwendetes Ordervolumen. |
| `CandleType` | `H1` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. |

## Notizen

- Die Strategie ruft `StartProtection()` einmal beim Start auf, damit StockSharp bei Bedarf die Liquidation von Notfallpositionen verwalten kann.
- Alle Protokolle und Kommentare werden auf Englisch bereitgestellt, während Beschreibungen der Sprache jeder README-Variante folgen, wie von der gefordert
Konvertierungsrichtlinien.
- Die Implementierung vermeidet Indikatorpuffer und ahmt den MetaTrader-Ansatz nach, indem nur das minimale rollierende Fenster beibehalten wird
notwendig, um ein Fraktal auszuwerten.
