# Expert610 Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Expert610 Breakout-Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `Expert610.mq4`. Der ursprüngliche Roboter wartet auf einen
breite Kerze und parkt dann sowohl eine Kauf-Stopp- als auch eine Verkaufs-Stopp-Order um den vorherigen Balken herum. Die Positionsgröße wird aus der abgeleitet
Prozentsatz des freien Kapitals, den der Händler zu riskieren bereit ist, und die Stop-Loss-/Take-Profit-Abstände werden in Pips ausgedrückt. Dies
Die Version StockSharp spiegelt dieses Verhalten wider, indem sie die übergeordnete API verwendet und dabei jeden Abstimmknopf als Strategieparameter verfügbar macht.

## Handelslogik
1. **Datenerfassung**
   - Die Strategie abonniert einen konfigurierbaren Kerzentyp und speichert den zuletzt fertiggestellten Balken.
   - Die Aktualisierung des Auftragsbuchs wird überwacht, um die aktuelle Geld-Brief-Spanne abzuschätzen. Wenn keine Tiefe verfügbar ist, wird der Spread-Beitrag angezeigt
Der Standardwert ist Null und reproduziert das ursprüngliche EA-Verhalten bei Brokern ohne Live-Spreads.
2. **Volatilitätsfilter**
   - Das vorherige Kerzenhoch minus dem aktuellen Schlusskurs und der aktuelle Schlusskurs minus dem vorherigen Tiefpunkt müssen beide höher sein
`ThresholdPips` (umgerechnet in absolute Preiseinheiten).
   - Die aktuelle Kerzenöffnung muss strikt unter dem vorherigen Hoch liegen, um ein Kauf-Setup zu ermöglichen, und strikt über dem vorherigen Tief
ein Verkaufs-Setup zulassen. Wenn beide Bedingungen erfüllt sind, führt der Algorithmus symmetrische Pending-Orders durch.
3. **Auftragserteilung**
   - Kaufstopps werden bei `previous high + BreakoutOffset + spread` platziert und entsprechen dem MT4-Code, bei dem der Briefkurs verwendet wird.
   - Verkaufsstopps werden bei `previous low - BreakoutOffset` platziert und bleiben dabei auch dem ursprünglichen Skript treu, das das ignoriert
Spread auf der Geldseite.
   - Es kann immer nur ein Paar ausstehender Aufträge aktiv sein. Wenn eine Bestellung bereits funktioniert, werden die neuen Signale übersprungen.
4. **Risikomanagement**
   - Die Losgröße ergibt sich aus dem freien Kapital (`Portfolio.CurrentValue - Portfolio.BlockedValue`) multipliziert mit
`RiskPercent / 100`. Der Betrag wird auf `RoundingDigits` gerundet und mit der gleichen Heuristik wie MT4 in Lots umgerechnet
Code: `lot = risk / stopPips * 0.1`, der davon ausgeht, dass ein Pip eines 0,1-Lots einer Einheit der Kontowährung entspricht.
   - Das berechnete Los wird an die Börsenlimits und den Parameter `MinimumVolume` angepasst, bevor es an den Veranstaltungsort gesendet wird.
   - `StartProtection` fügt jeder resultierenden Position preisbasierte Stopps und Ziele hinzu, sodass Füllungen sofort die erhalten
konfigurierte `StopLossPips`- und `TakeProfitPips`-Offsets.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `RoundingDigits` | Dezimalstellen, die beim Runden von Risiko- und Volumenberechnungen verwendet werden. | `2` | Darf nicht negativ sein. |
| `RiskPercent` | Prozentsatz des freien Kapitals, das bei jedem Eintrag riskiert wird. | `1` | Auf `0` setzen, um die dynamische Größenanpassung zu deaktivieren und auf `MinimumVolume` zurückzugreifen. |
| `MinimumVolume` | Harte Untergrenze für das ausstehende Auftragsvolumen. | `0.1` | Respektiert auch die Sicherheitsbestimmungen `MinVolume` und `VolumeStep`. |
| `ThresholdPips` | Mindestabstand vom letzten Schlusskurs zu den vorherigen Kerzenextremen. | `5` | Wird in Pips gemessen und mit der erkannten Pip-Größe umgerechnet. |
| `BreakoutOffsetPips` | Beim Bereitstellen von Aufträgen wird ein Puffer hinzugefügt, der über das vorherige Hoch/Tief hinausgeht. | `2` | Beidseitig symmetrisch aufgetragen. |
| `StopLossPips` | Stop-Loss-Distanz für ausgeführte Aufträge. | `5` | Wird in Pips ausgedrückt und an `StartProtection` gesendet. |
| `TakeProfitPips` | Mit ausgeführten Aufträgen verbundene Take-Profit-Distanz. | `10` | Ausgedrückt in Pips; auf `0` setzen, um das Ziel zu deaktivieren. |
| `CandleType` | Zur Bewertung des Ausbruchs verwendete Kerzenserie. | `1 hour` Zeitrahmen | Akzeptiert alle `DataType`, die von StockSharp unterstützt werden. |

## Implementierungshinweise
- Die Pip-Größe wird aus `PriceStep` und `Decimals` des Instruments abgeleitet (5-stellige und 3-stellige Forex-Symbole erhalten ein ×10
Anpassung), um die Konvertierung mit der MQL4-Formel identisch zu halten.
- Bei der Rundung der Auftragsgröße wird `VolumeStep` berücksichtigt, auf `MinVolume`/`MaxVolume` beschränkt und schließlich die Strategieebene durchgesetzt
`MinimumVolume`, sodass resultierende Anfragen immer handelbar sind.
- Bei der Spread-Kompensation wird der beste Bid/Ask aus dem abonnierten Orderbuch verwendet. Dies ergibt den gleichen Einstiegspreis wie der
MT4-Implementierung, wenn die Plattform Live-Spreads bereitstellt und ansonsten ordnungsgemäß heruntergefahren wird.
- Ausstehende Bestellungen werden aus dem internen Status gelöscht, sobald StockSharp sie als ausgeführt, storniert oder fehlgeschlagen meldet, sodass die
Logik, um bei der nächsten qualifizierten Kerze neue Aufträge zu erteilen.

## Unterschiede zur MQL-Version
- Der ursprüngliche EA rundete sowohl Risiko als auch Volumen mit `Digits2Round` ab. Der Port behält diese Funktion bei, richtet sie aber zusätzlich aus
Ergebnis zu börsenspezifischen Volumenschritten.
- Anstatt Schutzpreise direkt mit den ausstehenden Aufträgen zu verknüpfen, setzt die StockSharp-Strategie auf `StartProtection`
Jede gefüllte Position erhält automatisch Stop-Loss- und Take-Profit-Aufträge.
- Portfolioinformationen ersetzen die MT4-Funktionen `AccountBalance()` und `AccountMargin()`, um freies Kapital zu erhalten; wenn diese Daten
nicht verfügbar ist, greift die Strategie ordnungsgemäß auf die Größe `MinimumVolume` zurück.
- Alle Berechnungen beziehen sich nur auf fertige Kerzen, wodurch ein Neuzeichnen innerhalb des Balkens verhindert wird und die Tick-basierte Schleife `start()` abgeglichen wird
sobald die Bar schließt.
