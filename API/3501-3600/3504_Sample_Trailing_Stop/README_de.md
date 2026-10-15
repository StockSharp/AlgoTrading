# Beispiel einer Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **SampleTrailingStopStrategy** ist eine direkte C#-Portierung des MetaTrader-Expertenberaters `SampleTrailingstop.mq4`. Die Strategie generiert keine eigenen Einträge; Stattdessen überwacht es kontinuierlich die aktuelle Position und verwaltet schützende Stop-Loss- und Take-Profit-Orders. Die Logik spiegelt die ursprüngliche EA wider, indem sie die vom Broker auferlegten Stop- und Freeze-Levels respektiert und gleichzeitig einen in Preispunkten gemessenen Trailing Stop anwendet.

Immer wenn eine Long-Position profitabel wird und das beste Gebot weit genug vom Einstiegspreis entfernt ist, verschiebt die Strategie den Stop-Loss zunächst um den minimal zulässigen Abstand knapp unter das Gebot. Nachfolgende Aktualisierungen folgen dem Stopp hinter dem Gebot um die konfigurierte Anzahl von Punkten plus Broker-Puffer. Short-Positionen werden symmetrisch abgewickelt, wobei der Stop über der Briefmarke liegt. Optionale Take-Profit-Ziele werden bei jedem nachfolgenden Ereignis neu berechnet.

## Datenfluss

* Abonniert Level1-Updates, um die besten Geld-/Briefkurse zu erhalten.
* Verfolgt den aktuellen Positionspreis über die Basis `Strategy` API.
* Registriert schützende Stop- und Limit-Orders erneut, wenn neue Preise berechnet werden.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TrailingStopPoints` | `200` | Abstand zwischen dem Markt und dem Trailing Stop, gemessen in Preispunkten. Dieser Wert wird bei nachlaufenden Berechnungen zu den Brokerpuffern hinzugefügt. |
| `TakeProfitPoints` | `1000` | Optionale Take-Profit-Distanz in Punkten. Auf `0` setzen, um die Take-Profit-Verwaltung zu deaktivieren. |
| `StopLevelPoints` | `0` | Beschränkung des Broker-Stop-Levels, ausgedrückt in Punkten. Es wird zur Trailing-Distanz addiert, um die Gültigkeit der Stop-Orders zu gewährleisten. |
| `FreezeLevelPoints` | `0` | Beschränkung des Broker-Einfrierniveaus, ausgedrückt in Punkten. Beim Trailing wird gewartet, bis sich der Markt über diesen Puffer vom Einstiegspreis hinaus bewegt. |

Alle Abstände werden mit der Tick-Größe des Instruments in Preiswerte übersetzt, um das `_Point`-Verhalten von MetaTrader zu emulieren.

## Trailing-Algorithmus

1. **Positionsvalidierung** – Die Strategie ignoriert das Trailing, bis eine Position existiert und der beste Geld-/Briefkurs bekannt ist.
2. **Gewinnprüfung** – Trailing wird nur aktiviert, wenn die Position profitabel ist (`bid > entry` für Long-Positionen, `ask < entry` für Short-Positionen) und der Freeze-Puffer gelöscht wurde.
3. **Anfängliche Stop-Platzierung** – Wenn noch kein Trailing-Stop aktiv ist, wird der Stop auf den minimal zulässigen Abstand vom Markt verschoben (Geld minus Puffer für Long-Positionen, Briefkurs plus Puffer für Short-Positionen), sobald der Preis mindestens die Trailing-Distanz vom Einstieg entfernt ist.
4. **Trailing-Updates** – Während die Position profitabel bleibt, wird der Stop unter Verwendung der konfigurierten Trailing-Distanz plus Broker-Puffer tiefer gedrückt. Bei Aktivierung werden die Take-Profit-Werte bei jedem Update neu berechnet.
5. **Auftragsverwaltung** – Schutzaufträge werden automatisch über hochrangige Hilfsmethoden erstellt, aktualisiert oder storniert, sodass der Broker immer die neuesten Stop-Loss- und Take-Profit-Werte sieht.

## Nutzungshinweise

* Starten Sie die Strategie zusammen mit einer anderen Komponente, die Positionen eröffnet, oder verwenden Sie manuelle Aufträge; Dieses Modul verwaltet nur Exits.
* Stellen Sie sicher, dass die Metadaten des Instruments die richtigen Preis- und Volumenschritte enthalten. Die Strategie normalisiert jeden generierten Preis und Betrag, um Wechselkursbeschränkungen zu erfüllen.
* Wenn die Positionsrichtung umgedreht wird, werden alle alten Schutzbefehle aufgehoben, bevor die nachlaufenden Neustarts für die neue Seite erfolgen.
