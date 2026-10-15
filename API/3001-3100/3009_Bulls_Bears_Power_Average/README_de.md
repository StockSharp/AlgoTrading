# Bulls & Bears Power Durchschnitt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 5-Experten `MySystem.mq5` in `MQL/22016`.
- Erkennt Momentum-Umkehrungen durch Mittelung der Elder Bulls Power und Bears Power Werte, die aus Kerzenextremen und einer EMA berechnet werden.
- Tritt **long** ein, wenn der Durchschnitt steigt, während er noch unter null liegt (bärischer Druck lässt nach), und **short**, wenn der Durchschnitt sinkt, während er noch über null liegt (bullischer Druck lässt nach).
- Für jeweils eine offene Position konzipiert; Stop-Loss und Take-Profit sind optional und in Pips ausgedrückt.

## Indikatorlogik
| Komponente | Beschreibung |
|-----------|-------------|
| Exponential Moving Average (EMA) | Auf Kerzenschlusskurse angewendet. Der Parameter `MaPeriod` steuert das Glättungsfenster (Standard 5). |
| Bulls Power (abgeleitet) | Berechnet als `High - EMA`. Misst die bullische Stärke relativ zur EMA. |
| Bears Power (abgeleitet) | Berechnet als `Low - EMA`. Misst die bärische Stärke relativ zur EMA. |
| Durchschnittliche Kraft | `(Bulls Power + Bears Power) / 2`. Dieser synthetische Oszillator wird mit seinem vorherigen Wert verglichen, um Momentum-Änderungen zu erkennen. |

Die Strategie bewertet die letzten zwei fertigen Kerzen. Neue Trades werden nur bewertet, wenn eine Kerze vollständig abgeschlossen ist, um Intrabar-Rauschen zu vermeiden.

## Einstiegsregeln
1. Warten, bis die EMA vollständig gebildet ist (d.h. mindestens `MaPeriod` Kerzen verarbeitet hat).
2. Bulls Power und Bears Power für die gerade geschlossene Kerze unter Verwendung ihres Hochs/Tiefs und des EMA-Werts berechnen.
3. Beide Kräfte mitteln, um die aktuelle Oszillatorlesing zu erhalten.
4. Mit dem vorherigen Durchschnitt vergleichen:
   - **Long-Setup**: vorheriger Durchschnitt `<` aktueller Durchschnitt **und** aktueller Durchschnitt `< 0`. Long einsteigen, wenn keine bestehende Position vorhanden.
   - **Short-Setup**: vorheriger Durchschnitt `>` aktueller Durchschnitt **und** aktueller Durchschnitt `> 0`. Short einsteigen, wenn flat.
5. Nach dem Einstieg auf optionale Schutzorders oder manuelle Verwaltung verlassen. Der Algorithmus generiert keine Ausstiegssignale außer Stop-Loss/Take-Profit.

## Risikomanagement
- `StopLossPips`: Optionaler absoluter Stop-Abstand in Pips (0 deaktiviert den Stop). Umgerechnet mit dem `PriceStep` des Instruments.
- `TakeProfitPips`: Optionales absolutes Gewinnziel in Pips (0 deaktiviert das Ziel).
- Schutzorders werden registriert, sobald die Position über `StartProtection` mit Marktausführung eröffnet wird.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Ordergröße für Markteinstiege. |
| `StopLossPips` | 15 | Stop-Loss-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | 95 | Take-Profit-Abstand in Pips. Auf `0` setzen zum Deaktivieren. |
| `MaPeriod` | 5 | EMA-Länge für die Bulls/Bears Power Berechnung. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzenserie für alle Berechnungen (ändern, um Ihrem Datenfeed zu entsprechen). |

## Verwendungshinweise
1. Die Strategie einem Instrument zuordnen und sicherstellen, dass `CandleType` dem beabsichtigten Zeitrahmen entspricht.
2. `OrderVolume`, `StopLossPips` und `TakeProfitPips` an die Broker-Anforderungen anpassen.
3. Die Strategie starten; sie abonniert automatisch Kerzen, aktualisiert die EMA und gibt Marktorders bei qualifizierenden Signalen aus.
4. Es kann jeweils nur eine Position existieren. Wenn ein Trade aktiv ist, werden neue Signale ignoriert, bis die Schutzorders die Position schließen oder sie manuell geschlossen wird.
5. Da die ursprüngliche MQL-Version `InpBarCurrent = 1` verwendete, arbeitet dieser Port immer auf vollständig geschlossenen Kerzen und vergleicht aufeinanderfolgende Werte; keine Intrabar-Neuberechnung wird durchgeführt.

## Unterschiede zum Original-MQL-Experten
- Verwendet die StockSharp High-Level `Strategy`-API mit Kerzenabonnements und Indikatorbindung statt manuellem Buffer-Zugriff.
- Leitet Pips automatisch von `PriceStep` ab anstatt manuelle Ziffernanpassungen.
- Überspringt das ursprünglich auskommentierte Order-Management und verlässt sich auf eingebauten Positionsschutz.
- Behält die Einzelpositions-Einschränkung der Quelle bei, indem Signale ignoriert werden, während eine Position existiert.

## Testempfehlungen
- Backtest auf dem beabsichtigten Symbol/Zeitrahmen mit historischen Daten, die Hoch-/Tief-Preise für genaue Bulls/Bears-Berechnung beinhalten.
- Schutzorder-Verhalten mit Ihrer Broker-Tick-Größe und Volumen-Schritt validieren, bevor live ausgeführt wird.
- Mit verschiedenen `MaPeriod`-Werten experimentieren, um die Sensitivität an die Instrumentvolatilität anzupassen.
