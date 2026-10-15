# Spreader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Spreader-Strategie** ist ein Pair-Trading-Ansatz, der vom ursprünglichen MetaTrader-Expert-Advisor „Spreader" inspiriert wurde. Die Strategie überwacht zwei positiv korrelierte Instrumente und versucht, von kurzfristigen Divergenzen zu profitieren, während ein marktneutrales Profil aufrechterhalten wird. Sobald die kombinierte Position das gewünschte Geldertragsziel erreicht, schließt die Strategie beide Beine und wartet auf das nächste Setup.

Der Algorithmus ist standardmäßig für Einminutenkerzen ausgelegt und spiegelt das Verhalten des Original-EA wider, aber der Zeitrahmen kann angepasst werden, wenn die Strategie in Designer, Shell oder den API-Runner geladen wird.

## Handelslogik

1. **Datenvorbereitung**
   - Abonniert Kerzen für das primäre Wertpapier (das der Strategie zugewiesene) und das sekundäre Wertpapier.
   - Speichert die letzten `2 * ShiftLength + 1` Schlusskurswerte für jedes Instrument. Die Standard-Shift-Länge beträgt 30 Bars.
   - Reagiert nur auf abgeschlossene Kerzen und erfordert, dass beide Instrumente eine Bar mit der gleichen Eröffnungszeit erzeugen.

2. **Trendfilter**
   - Berechnet die Preisänderungen zwischen dem aktuellen Schluss und dem Schluss `ShiftLength` Bars zuvor sowie die Änderung zwischen den mittleren und ältesten Samples für beide Instrumente.
   - Wenn die zwei Änderungen für eines der Instrumente das gleiche Vorzeichen haben, interpretiert die Strategie dies als anhaltenden Trend und überspringt den Handel.

3. **Korrelationsprüfung**
   - Stellt sicher, dass das Vorzeichen der letzten Änderung bei beiden Instrumenten identisch ist. Wenn das Vorzeichen abweicht, gilt die Korrelation als negativ und kein Spread wird eröffnet.

4. **Volatilitätsausrichtung**
   - Berechnet den absoluten Betrag der jüngsten Schwankungen (`a` für das primäre Bein, `b` für das sekundäre Bein) und verwendet ihr Verhältnis zur Skalierung des Absicherungsvolumens. Verhältnisse außerhalb des Bereichs `[0.3, 3]` werden abgelehnt, da sie instabiles Verhalten anzeigen.

5. **Einstieg**
   - Wählt die Richtung des primären Beins durch Vergleich der normalisierten Schwankungen: Wenn die primäre Bewegung stärker ist, kauft die Strategie das primäre Instrument und verkauft das sekundäre Bein; andernfalls verkauft sie das primäre Bein und kauft das sekundäre.
   - Orders werden mit Marktausführung gesendet und Volumina werden normalisiert, um den Lot-Schritt sowie Mindest- und Höchstgrenzen jedes Wertpapiers einzuhalten.

6. **Positionsmanagement**
   - Wenn nur das sekundäre Bein offen ist (z.B. aufgrund von Konnektivitätsproblemen), öffnet die Strategie das fehlende primäre Bein in der entgegengesetzten Richtung, um die Absicherung wiederherzustellen.
   - Wenn nur das primäre Bein verbleibt, wird es sofort geschlossen, um direktionales Exposure zu vermeiden.
   - Wenn beide Beine vorhanden sind, überwacht die Strategie den kombinierten variablen Gewinn und schließt beide Positionen, sobald das konfigurierte Geldertragsziel erreicht ist.

7. **Sicherheitsprüfungen**
   - Der Handel ist deaktiviert, wenn die Kontraktgröße (Multiplikator) der beiden Wertpapiere unterschiedlich ist, da der Original-EA gleiche Kontraktspezifikationen voraussetzt.
   - Alle Handelsanfragen werden ignoriert, bis die Strategie verbunden, synchronisiert und durch die Hostingumgebung handelsberechtigt ist (`IsFormedAndOnlineAndAllowTrading`).

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `SecondSecurity` | Instrument, das das Absicherungsbein des Spreads bildet. Dieser Parameter ist erforderlich. |
| `PrimaryVolume` | Basis-Order-Volumen für das primäre Instrument. Das sekundäre Volumen wird mit dem Schwankungsverhältnis skaliert. |
| `TargetProfit` | Absoluter Gewinn, ausgedrückt in der Kontowährung, nach dem beide Beine geschlossen werden. |
| `ShiftLength` | Anzahl der Bars, die beim Vergleich der jüngsten Schwankungen verwendet werden. Die Strategie verwendet `2 * ShiftLength + 1` Kerzen von jedem Instrument. |
| `CandleType` | Kerzenserie für die Analyse. Standardmäßig 1-Minuten-Zeitrahmen. |

## Tipps

- Die Strategie funktioniert am besten bei Instrumenten mit stabiler positiver Korrelation und ähnlichen Volatilitätsprofilen (z.B. eng verwandte Währungspaare oder Indexfutures).
- Kontraktspezifikationen sollten ausgerichtet sein (Tick-Größe, Lot-Schritt, Margin); andernfalls kann die Volumen-Normalisierung die Ordergrößen erheblich reduzieren.
- Da die Strategie auf Kerzendaten basiert, stellen Sie sicher, dass beide Instrumente synchronisierte Bar-Updates vom Datenanbieter erhalten.

## Anforderungen

- Zwei liquide Instrumente mit positiver Korrelation.
- Zugang zu Marktdaten und Handelsberechtigungen für beide Instrumente über StockSharp-Konnektoren.
- Portfolio der Strategieinstanz zugewiesen.
