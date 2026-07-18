# Typische Breakout-Strategie für den Montag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Monday-Typical-Breakout-Strategie** ist eine C#-Portierung des MetaTrader-Expertenberaters `yi1ywioff50qr6` (Repository-ID 8187). Der ursprüngliche Roboter überwacht stündliche Kerzen und eröffnet jeden Montag eine Long-Position, wenn die neue Sitzung über dem typischen Preis des vorherigen Balkens `(high + low + close) / 3` beginnt. Diese Implementierung reproduziert die Einstiegslogik innerhalb des StockSharp-Strategierahmens auf hoher Ebene und fügt detaillierte Konfigurationsparameter für die Positionsgröße und Risikokontrolle hinzu.

## Handelslogik

1. Die Strategie abonniert die konfigurierte Kerzenserie (standardmäßig stündlich).
2. Zu Beginn jeder fertigen Kerze wird geprüft, ob:
   - Die Kerze gehört zum Montag.
   - Die Öffnungszeit der Kerze entspricht dem konfigurierten Parameter *Open Hour* (Standard 09:00).
   - Es sind keine offenen Positionen oder aktiven Aufträge vorhanden.
   - Der Eröffnungspreis der Kerze ist höher als der typische Preis des vorherigen Balkens.
3. Wenn alle Bedingungen erfüllt sind, sendet die Strategie eine Marktkauforder mit einem vom Money-Management-Block berechneten Volumen. Durch `StartProtection` werden schützende Stop-Loss- und Take-Profit-Distanzen angewendet.

Die Strategie eröffnet niemals Short-Positionen und platziert nur einen Trade pro qualifizierter Montagskerze.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FixedVolume` | Losgröße für Einträge. Auf `0` setzen, um die Equity-Skalierungstabelle zu aktivieren. | `0.1` |
| `OpenHour` | Handelssitzungsstunde (0-23), in der Montagssignale ausgewertet werden. | `9` |
| `StopLossPoints` | Abstand in Preispunkten für den Schutzstopp. `0` deaktiviert den Stopp. | `50` |
| `TakeProfitPoints` | Abstand in Preispunkten zum Gewinnziel. `0` deaktiviert das Ziel. | `20` |
| `InitialEquity` | Eigenkapitalschwelle, die die eigenkapitalbasierte Losskalierung aktiviert. | `600` |
| `EquityStep` | Zur Erhöhung der Handelsgröße ist eine Eigenkapitalerhöhung erforderlich. | `300` |
| `InitialStepVolume` | Die Losgröße wird verwendet, wenn das Eigenkapital mindestens `InitialEquity` beträgt. | `0.4` |
| `VolumeStep` | Zusätzliche Losgröße für jeden erreichten `EquityStep` hinzugefügt. | `0.2` |
| `CandleType` | Kerzendatentyp, der die Strategie steuert (standardmäßig stündlich). | `1 hour time-frame` |

## Money-Management

- Wenn `FixedVolume` größer als Null ist, verwendet die Strategie immer die feste Losgröße.
- Wenn `FixedVolume` gleich Null ist, prüft die Strategie das Portfolio-Eigenkapital:
  - Wenn das Eigenkapital unter `InitialEquity` liegt, wird die Mindestmenge des Instruments verwendet.
  - Andernfalls beginnt das Volumen bei `InitialStepVolume` und erhöht sich um `VolumeStep` für jedes `EquityStep` zusätzliches Eigenkapital.
  - Die endgültige Lautstärke richtet sich nach den Mindest- und Stufenbeschränkungen des Instruments.

## Risikomanagement

`StartProtection` wird während `OnStarted` aktiviert. Die Stop-Loss- und Take-Profit-Abstände werden mithilfe des Instruments `PriceStep` automatisch von Punkten in Preis-Offsets übersetzt. Setzen Sie einen der beiden Distanzen auf Null, um diese Komponente zu deaktivieren.

## Nutzungshinweise

- Das Original EA ist für Stundenkerzen konzipiert. Bei kürzeren Zeitrahmen können mehrere Montagskerzen zur gleichen Stunde entstehen. Der Port behält das Verhalten eines einzelnen Eintrags pro Kerze bei und ignoriert weiterhin zusätzliche Signale, während eine Position offen ist.
- Stellen Sie sicher, dass die Portfolioinformationen (`Portfolio.CurrentValue`) verfügbar sind, wenn der dynamische Größenblock aktiviert ist.
- Die Strategie erfordert Level-1-Daten zur Ausführung von Marktaufträgen und das entsprechende Kerzenabonnement für den konfigurierten `CandleType`.

## Konvertierungshinweise

- Die Filterung nach magischen Zahlen von MQL wird durch die Positions- und Reihenfolgeprüfungen von StockSharp (`Position` und `ActiveOrders`) ersetzt.
- Zeitvergleiche nutzen `DateTimeOffset` aus der Kerzeneröffnungszeit mit `.ToLocalTime()`, um mit der Chartzeit in Einklang zu bleiben.
- Schutzbestellungen werden vom High-Level-Helper `StartProtection` statt einer manuellen Auftragserteilung bearbeitet.
