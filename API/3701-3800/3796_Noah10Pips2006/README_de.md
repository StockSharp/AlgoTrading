# Noah 10 Pips 2006 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Stellt die Ausbruchs- und Umkehrlogik des ursprünglichen Noah10pips2006 MetaTrader 4-Expertenberaters wieder her.
- Baut Preiskanäle der vorherigen Sitzung auf und platziert Stop-Orders etwa in der Mitte.
- Wendet sicheres Gewinn-Trailing, optionale dynamische Positionsgrößenbestimmung und einen optionalen Umkehrhandel nach dem Schließen der ersten Position an.

## Handelslogik
1. **Berechnung der Sitzungsreichweite**
Zu Beginn jedes neuen Handelstages (nach Anwendung des konfigurierten Zeitzonenversatzes) zeichnet die Strategie die Höchst- und Tiefststände der vorherigen Sitzung auf. Diese Ebenen werden zur Berechnung verwendet:
   - Der Mittelpunkt zwischen Hoch und Tief.
   - Ein „Pass“-Puffer, der 20 Pips über/unter der Spanne liegt.
   - Ein Einstiegskanal, der durch Subtrahieren/Addieren von 40 Pips (oder 25 % der Range, wenn die Range größer als 160 Pips ist) entsteht.
2. **Erste ausstehende Bestellung**
Wenn der Markt in das Handelsfenster eintritt, prüft die Strategie den letzten Schlusskurs:
   - Liegt der Schlusskurs zwischen dem Mittelpunkt und dem oberen Puffer, wird am Mittelpunkt ein **Verkaufsstopp** gesetzt.
   - Liegt der Schlusskurs zwischen dem unteren Puffer und dem Mittelpunkt, wird ein **Kaufstopp** am Mittelpunkt platziert.
Die Sortimentsbreite muss das konfigurierte Minimum überschreiten, bevor Bestellungen aufgegeben werden können.
3. **Zweite ausstehende Bestellung**
Bleibt nur noch eine Stop-Order aktiv, fügt das System die gegenläufige Order am entsprechenden Puffer hinzu (oberer Puffer für einen Kauf-Stopp, unterer Puffer für einen Verkaufs-Stopp). Dies spiegelt das ursprüngliche EA-Verhalten wider und bereitet die Strategie auf Ausbrüche auf beiden Seiten der Spanne vor.
4. **Positionsmanagement**
   - Schützende Stop-Loss- und Take-Profit-Orders werden erstellt, nachdem ein Eintrag ausgeführt wurde.
   - Sobald der variable Gewinn die sichere Auslöseschwelle erreicht, wird der Stop-Loss verschoben, um den konfigurierten sicheren Gewinn zu sichern.
   - Wenn die sichere Sperre aktiv ist, folgt ein optionaler Trailing Stop dem Preis im angegebenen Abstand.
5. **Täglicher Shutdown**
Alle ausstehenden Aufträge und offenen Positionen werden geschlossen, wenn das Handelsfenster endet oder der Annahmeschluss am Freitag erreicht ist.
6. **Umkehrhandel**
Die erste abgeschlossene Position kann eine Marktorder in die entgegengesetzte Richtung auslösen, wodurch das „Reverse After Stop“-Verhalten des ursprünglichen Codes reproduziert wird. Die Rückabwicklung wird übersprungen, wenn durch die sichere Gewinnanpassung bereits Gewinne gesichert sind.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzenserien dienen zur Steuerung von Berechnungen und Timing. Standard: 1-Stunden-Kerzen. |
| `TimeZoneOffset` | Verschiebung (in Stunden), die vor täglichen Berechnungen auf Austauschzeitstempel angewendet wird. |
| `StartHour`, `StartMinute` | Öffnungszeit des Handelsfensters in der verschobenen Zeitzone. |
| `EndHour`, `EndMinute` | Schließzeit des Handelsfensters. Neue Einträge werden nicht nachträglich platziert. |
| `FridayEndHour` | Stunde am Freitag, wenn Positionen zwangsweise geschlossen werden. |
| `TradeFriday` | Aktiviert oder deaktiviert die Eröffnung neuer Positionen am Freitag. |
| `StopLossPips`, `TakeProfitPips` | Abstand (in Pips) der nach der Eingabe erstellten Schutzbefehle. |
| `TrailingStopPips` | Trailing-Stop-Distanz, die nach dem Secure-Profit-Schritt verwendet wird. Auf 0 setzen, um das Nachziehen zu deaktivieren. |
| `SecureProfitPips` | Der Gewinn wird gesperrt, wenn der sichere Auslöser aktiviert wird. |
| `TrailSecureProfitPips` | Erforderlicher Gewinnschwellenwert, bevor der Stopp auf das sichere Niveau verschoben wird. |
| `MinimumRangePips` | Mindestbreite des Eingangskanals, die für die Auftragserteilung erforderlich ist. |
| `StartYear`, `StartMonth` | Ignorieren Sie Marktdaten, die älter als dieses Datum sind. |
| `MinVolume`, `MaxVolume` | Auf das berechnete Auftragsvolumen angewendete Grenzen. |
| `MaximumRiskPercent` | Prozentsatz des Portfoliowerts, der pro Trade riskiert wird, wenn die dynamische Größenanpassung aktiviert ist. |
| `FixedVolume` | Bei `true` verwendet die Strategie die Eigenschaft `Volume` anstelle des Risikomodells. |

## Praktische Hinweise
- Das Instrument muss gültige `PriceStep`- und `StepPrice`-Werte liefern, wenn der risikobasierte Positionsgrößenmodus verwendet wird.
- Trailing- und Secure-Profit-Anpassungen basieren auf abgeschlossenen Kerzen, sodass Intrabar-Füllungen bei der nächsten abgeschlossenen Kerze verarbeitet werden.
- Die Strategie storniert und ersetzt Schutzaufträge, wann immer die abschließende Logik den Stop-Preis aktualisiert.
- Stellen Sie sicher, dass der Zeitzonenversatz mit der Quelle der historischen Daten übereinstimmt. Andernfalls kann die Spanne des Vortages vom ursprünglichen MT4-Experten abweichen.

## Konvertierungsvorbehalte
- Visuelle Zeichenobjekte aus der MT4-Version wurden weggelassen; Verwenden Sie die bereitgestellten Ebenen oder fügen Sie bei Bedarf benutzerdefinierte Diagrammanmerkungen hinzu.
- Der Algorithmus geht bei der Konvertierung der festen 20/40-Pip-Puffer von vierstelligen Forex-Kursen aus; Passen Sie Parameter für verschiedene Anlageklassen an.
- Reverse Trades werden zum Marktwert mit dem aktuellen Volumenmodell ausgeführt und entsprechen dem Verhalten des ursprünglichen EA nach dem Löschen entgegengesetzter ausstehender Orders.
