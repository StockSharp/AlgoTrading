# Casino111-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Casino111 ist ein Gegentrend-Breakout-System, das vom gleichnamigen MetaTrader 4-Expertenberater stammt. Bei jedem neuen Balken vergleicht die Strategie den aktuellen Eröffnungspreis mit Referenzniveaus, die von der vorherigen Tageskerze abgeleitet wurden. Wenn die offenen Lücken über die täglichen Extremwerte (plus konfigurierbare Puffer) hinausgehen, eröffnet der Algorithmus sofort eine Marktposition in die entgegengesetzte Richtung und verlässt sich auf einen symmetrischen Stop-Loss-/Take-Profit-Schutz. Der StockSharp-Port behält das Einzelpositionsverhalten des ursprünglichen Roboters bei und fügt umfangreiche Parametrisierung für Forschung und Optimierung hinzu.

## Ein- und Ausstiegslogik
1. Die vorherigen Tageshöchst- und -tiefstwerte werden aus einem speziellen täglichen Kerzenabonnement abgerufen. Zwei Offsets (`UpperOffsetPoints` und `LowerOffsetPoints`), ausgedrückt in MetaTrader Punkten, erweitern den Referenzkanal.
2. Bei jeder abgeschlossenen Handelskerze prüft die Strategie die vorherigen und aktuellen Eröffnungen:
   - Wenn die neue Eröffnung über das Tageshoch plus den oberen Offset springt, wird eine **Short**-Position eröffnet (Fading der Lücke).
   - Wenn die neue Eröffnung unter das Tagestief abzüglich des unteren Offsets fällt, wird eine **Long-Position** eröffnet.
3. Es ist jeweils nur eine Position zulässig. Alle aktiven Aufträge müssen ausgeführt werden, bevor ein neues Signal berücksichtigt wird.
4. `StartProtection` spiegelt den ursprünglichen festen Stop und das Take-Ziel wider, die beide `BetPoints` vom Einstiegspreis entfernt liegen (umgerechnet in Preisschritte).

## Geldmanagement
- `UseMoneyManagement = false` hält die Handelsgröße fest (`BaseVolume`).
- `UseMoneyManagement = true` aktiviert die Martingal-Progression, die im MT4-Code zu sehen ist:
  - Nach jedem Verlust- oder Break-Even-Trade wird das nächste Ordervolumen mit `(BetPoints * 2) / (BetPoints - spreadPoints)` multipliziert.
  - Der Spread wird anhand der neuesten besten Geld-/Briefkurse geschätzt, die über das Orderbuchabonnement gesammelt wurden. Wenn keine Kurse verfügbar sind, beträgt der Multiplikator standardmäßig `2`.
  - Durch Siege wird die Positionsgröße auf `BaseVolume` zurückgesetzt. Alle Bände sind auf das Instrument `VolumeStep` ausgerichtet und durch `MaxVolume` begrenzt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Erlauben Sie lange Einträge, die durch Lücken unterhalb des Tageskanals ausgelöst werden. |
| `EnableSell` | `bool` | `true` | Erlauben Sie Short-Einstiege, die durch Lücken über dem Tageskanal ausgelöst werden. |
| `BetPoints` | `decimal` | `400` | Symmetrische Stop-Loss- und Take-Profit-Distanz in MetaTrader Punkten (umgerechnet in Preisschritte für StockSharp). |
| `UpperOffsetPoints` | `decimal` | `97` | Über dem vorherigen Tageshoch hinzugefügter Puffer, um rückläufige Gap-Umkehrungen zu erkennen. |
| `LowerOffsetPoints` | `decimal` | `77` | Der Puffer wurde unter das vorherige Tagestief subtrahiert, um bullische Gap-Umkehrungen zu erkennen. |
| `UseMoneyManagement` | `bool` | `false` | Aktivieren Sie die Losprogression im Martingal-Stil. |
| `MaxVolume` | `decimal` | `4` | Obergrenze, die auf das berechnete Volumen angewendet wird, wenn das Geldmanagement aktiv ist. |
| `BaseVolume` | `decimal` | `0.1` | Die anfängliche Ordergröße wird nach einem profitablen Trade oder bei deaktiviertem Money Management verwendet. |
| `CandleType` | `DataType` | `H1` | Primärer Zeitrahmen zur Bewertung der Open-Gap-Bedingungen (Standard ist 1 Stunde). |
| `DailyCandleType` | `DataType` | `D1` | Kerzentyp, der das Hoch/Tief des Vortages liefert (Standard ist 1 Tag). |

## Hinweise zur Implementierung
- Die Strategie basiert auf dem hochrangigen API von StockSharp: `SubscribeCandles` stellt sowohl den Handel als auch die täglichen Streams bereit, während `SubscribeOrderBook` den neuesten Spread für den Money-Management-Multiplikator beibehält.
- `StartProtection` verwaltet sowohl die Stop-Loss- als auch die Take-Profit-Komponente, sodass jeder Einstieg sofort symmetrische Ausstiege erhält, genau wie in MT4.
- Englische Inline-Kommentare heben jeden Entscheidungspunkt hervor, um die Wartung zu erleichtern.
- Bei allen Berechnungen wird die Suche nach der Indikatorhistorie vermieden. Es sind nur die aktuellen Kerzenöffnungswerte erforderlich, was die Logik `Time[0]` / `Open[0]` von MetaTrader widerspiegelt.

## Anwendungstipps
- Wählen Sie einen Handelszeitraum, der zu Ihrer Studie passt. Die standardmäßigen einstündigen Kerzen replizieren das übliche MT4-Setup, es können jedoch alle von StockSharp unterstützten `DataType` bereitgestellt werden.
- Stellen Sie bei der Verwendung der Geldverwaltung sicher, dass `MaxVolume` die Broker-Limits einhält; Der Ausrichtungshelfer begrenzt das Ergebnis auf `VolumeStep`, `MinVolume` und `MaxVolume`.
- Da das System immer höchstens eine Position offen hält, lässt es sich gut mit StockSharp-Diagrammen kombinieren, die Ein-/Ausstiegsmarkierungen zur manuellen Überprüfung darstellen.
- Testen Sie die Strategie in einer Replay-Umgebung, bevor Sie sie mit einem Live-Veranstaltungsort verbinden – der Gap-Fading-Ansatz ist aggressiv und hängt von zuverlässigen Spreads ab.
