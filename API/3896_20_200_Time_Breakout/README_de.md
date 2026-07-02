# Strategie Twenty200 Time Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader Expert Advisors **20/200 Expert v4.2 (AntS)**. Es wartet auf eine bestimmte Stunde des Handelstages und vergleicht dann zwei historische stündliche Eröffnungspreise (6 und 2 Balken zurück in der Standardkonfiguration). Wenn die entfernte Eröffnung um mehr als `Short Delta` Pips höher ist als die nähere Eröffnung, wird die Strategie verkauft, während die umgekehrte Lücke, die `Long Delta` Pips überschreitet, eine Long-Position eröffnet.

## Handelslogik

- Die Strategie abonniert stündliche Kerzen (konfigurierbar über `Candle Type`).
- Es ist nur ein Handel pro Tag erlaubt. Bestellungen werden aufgegeben, wenn eine Kerze mit einer Stunde von `Trade Hour` aktiv wird.
- Signale verwenden den Eröffnungspreis `LookbackFar` und `LookbackNear` Balken zurück von der aktuellen Kerze.
  - **Kurze Einrichtung:** `Open[t1] - Open[t2] > Short Delta × pip`.
  - **Lange Einrichtung:** `Open[t2] - Open[t1] > Long Delta × pip`.
- Es wird eine Market-Order mit dem berechneten Volumen versendet. Stop-Loss- und Take-Profit-Distanzen werden aus der MetaTrader-Version übernommen und in Pips ausgedrückt, die über `Security.PriceStep` automatisch in Preise umgewandelt werden.
- Es kann jeweils nur eine Position existieren. Der tägliche Handel wird am nächsten Kalendertag wieder aufgenommen.

## Positionsmanagement

- Stop-Loss und Take-Profit werden bei jeder Kerzenaktualisierung anhand der Hoch-/Tief-Extreme der Kerze bewertet.
- `Max Open Hours` erzwingt einen Marktausstieg, wenn die Positionslebensdauer die konfigurierte Anzahl von Stunden überschreitet (standardmäßig 504 Stunden). Setzen Sie den Parameter auf Null, um den Sicherheitstimer zu deaktivieren.

## Geldmanagement

- `Fixed Volume` definiert die Ersatzvertragsgröße, die verwendet wird, wenn `Use Auto Lot` deaktiviert ist oder die Kontostandinformationen nicht verfügbar sind.
- Wenn `Use Auto Lot` aktiviert ist, folgt die Losgröße der enormen Schritttabelle des Fachberaters. In StockSharp wird die Tabelle durch `volume = round(balance × Auto Lot Factor, 2)` mit dem Standardfaktor `0.000038` angenähert, wodurch die MT4-Werte innerhalb eines Pip Volumens über den dokumentierten Bereich (300 USD bis 270.000 USD+) reproduziert werden.
- Wenn der aktuelle Portfoliowert unter den zuletzt aufgezeichneten Saldo fällt, wird der nächste Trade mit `Big Lot Multiplier` multipliziert, was den „Big Lot“-Recovery-Trade im Originalcode nachahmt.
- Die Volumina werden auf `Security.VolumeStep` ausgerichtet und zwischen `MinVolume`/`MaxVolume` eingeklemmt, sofern verfügbar.

## Unterschiede zum MetaTrader EA

- Das MT4-Skript speicherte mehr als tausend manuelle Schwellenwertzeilen. Die StockSharp-Version verwendet einen linearen Koeffizienten (`Auto Lot Factor`), der zur gleichen Treppe passt. Passen Sie den Faktor an, wenn Sie eine exakte Replik für einen anderen Broker benötigen.
- Stop-Loss-/Take-Profit-Orders werden durch Marktaustritte bei Candle-Extremen simuliert. Dadurch bleibt das Verhalten über Backtests und Live-Handel hinweg konsistent, ohne auf die Stop-Order-Unterstützung der Börse angewiesen zu sein.
- Globale Variablen (`globalBalans`, `globalPosic`) werden durch den speicherinternen Status ersetzt. Es ist kein Dateisystem oder Terminalstatus erforderlich.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| Long/Short-Take-Profit | Abstand in Pips für Gewinnziele. |
| Long/Short-Stop-Loss | Abstand in Pips für Stop-Losses. |
| Handelsstunde | Stunde der Sitzung (0–23), in der Signale ausgelöst werden können. |
| Fern-/Nah-Rückblick | Wie viele Bars zurück, um die beiden Eröffnungspreise zu überprüfen? |
| Langes/Kurz-Delta | Erforderlicher Pip-Gap zum Eröffnen einer Position. |
| Maximale Öffnungszeiten | Maximale Positionslebensdauer in Stunden (0 deaktiviert den Schutz). |
| Feste Lautstärke | Basisvertragsvolumen, wenn die automatische Größenanpassung deaktiviert ist. |
| Verwenden Sie Auto-Lot | Losgröße anhand des Kontowerts aktivieren. |
| Automatischer Losfaktor | Auf den Portfoliowert angewendeter Multiplikator, um die MT4-Schritttabelle zu emulieren. |
| Großer Lot-Multiplikator | Nach einem Eigenkapitalrückgang wird ein Volumenmultiplikator angewendet. |
| Kerzentyp | Zeitrahmen, der für die Signalkerzen verwendet wird. |
