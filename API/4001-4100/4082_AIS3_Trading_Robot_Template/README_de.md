# AIS3-Handelsroboter-Vorlage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die AIS3 Trading Robot-Vorlage ist ein MetaTrader-Breakout-System, das auf zwei koordinierten Zeitrahmen basiert. Der primäre Zeitrahmen
erfasst die Struktur der vorherigen Kerze, während ein sekundärer Zeitrahmen die aktuelle Volatilität misst, um nachfolgende Aktualisierungen zu kontrollieren.
Dieser StockSharp-Port reproduziert originalgetreu die ursprüngliche Auftragsgröße, Eingabeprüfungen und abschließende Logik, ist jedoch implementiert
oben auf der übergeordneten Strategie API, sodass es in Designer, Shell oder einem beliebigen benutzerdefinierten StockSharp-Host ausgeführt werden kann.

## Handelsablauf
- **Marktdatenabonnements**: Die Strategie abonniert zwei Kerzenserien. Die primäre Serie (Standard 15 Minuten) bietet
das vorherige Kerzenhoch, -tief, -schluss, -mittelpunkt und -bereich. Die sekundäre Serie (Standard 1 Minute) misst den verwendeten schnellen Bereich
für Trailing Stops. Ein Live-Orderbuch-Feed hält die aktuell besten Geld-/Briefkurse mit den ursprünglichen MQL `MarketInfo` synchronisiert.
Anfragen.
- **Breakout-Validierung**:
  - Ein Long-Setup wird ausgelöst, wenn der vorherige Schlusskurs über dem Mittelpunkt liegt und der aktuelle Briefkurs über den vorherigen ausbricht
hoch plus die gemessene Spanne. Der Einstiegspreis ist der aktuelle Briefkurs.
  - Ein Short-Setup erfordert, dass der vorherige Schlusskurs unter dem Mittelpunkt bleibt und der Geldkurs das vorherige Tief durchbricht. Der Eintrittspreis
ist das aktuelle Gebot.
  - Beide Richtungen übernehmen die Broker-Sicherheitsüberprüfungen aus der Vorlage: den Abstand zwischen der Einfahrt und dem geplanten Stopp/Ziel
muss den konfigurierten Stop-Puffer überschreiten und der Stop muss auch nach Addition auf der richtigen Seite des Einstiegspreises bleiben
verbreiten.
- **Schutzanordnungen**:
  - Die Stop-Loss-Distanz beträgt `primaryRange × StopMultiplier` und ist oberhalb (für Long-Positionen) bzw. unterhalb (für Short-Positionen) verankert
Breakout-Kerze, wie im Integrationshandbuch beschrieben.
  - Die Take-Profit-Distanz beträgt `primaryRange × TakeMultiplier` und wird vom Einstiegspreis in Handelsrichtung platziert.
- **Handelsmanagement**:
  - Wenn eine Position offen ist, definiert der sekundäre Zeitrahmenbereich multipliziert mit `TrailMultiplier` die Nachlaufdistanz.
  - Der Trailing-Stop wird nur aktualisiert, wenn der Handel profitabel ist und das neue Niveau weiter entfernt ist als der konfigurierte Freeze- und Stop-Wert
Puffern und die Entfernung zwischen der aktuellen und der geplanten Haltestelle überschreitet `TrailStepMultiplier × spread`. Dies spiegelt die wider
Die Vorlage erfordert, dass der Preis um mindestens einen Trail-Schritt steigen muss, bevor der Stop geändert wird.
  - Positionen werden mit Marktaufträgen geschlossen, wenn der Geld-/Briefkurs die gespeicherten Stop-Loss- oder Take-Profit-Werte berührt.

## Risikomanagement
- **Kontoreserve**: `AccountReserve` hält einen Bruchteil des Portfolio-Eigenkapitals gesperrt. Die Strategie weigert sich, neue Positionen zu eröffnen
wenn das reservierte Kapital das beantragte Auftragsbudget unterschreiten würde. Dies entspricht dem Verhalten der Vorlage, bei dem das Risiko besteht
Die Rücklage schützt das Konto vor kaskadierenden Verlusten.
- **Orderreserve**: `OrderReserve` steuert den Teil des verbleibenden Kapitals, der pro Trade riskiert werden darf. Die Positionsgröße
wird als `riskBudget / |entry - stop|` berechnet und dann an den Sicherheitsvolumenschritt angepasst. Wenn keine Portfoliokennzahlen vorhanden sind
verfügbar, wird stattdessen der Fallback-Parameter `BaseVolume` verwendet.
- **Puffer stoppen und einfrieren**: `StopBufferTicks` und `FreezeBufferTicks` übersetzen Broker-Stoppbeschränkungen (z. B. `MODE_STOPLEVEL`
und `MODE_FREEZELEVEL` von MetaTrader) mithilfe der Wertpapierpreisstufe in Preiseinheiten umwandeln. Sie verhindern, dass die Strategie umgesetzt wird
Aufträge, die gegen Wechselkursbeschränkungen verstoßen oder den Trailing Stop zu aggressiv verschieben würden.
- **Multiplikator für nachfolgende Schritte**: `TrailStepMultiplier` spiegelt die Konstante `acd.TrailStepping` aus der MQ4-Vorlage wider. Es sorgt dafür
dass nachlaufende Aktualisierungen nur dann erfolgen, wenn der neue Stopp mindestens ein Spread-Vielfaches vom vorherigen Wert entfernt ist.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `AccountReserve` | Als Sicherheitsreserve gehaltener Anteil des Eigenkapitals (0–0,95).
| `OrderReserve` | Anteil des handelbaren Eigenkapitals, der dem Risikobudget pro Trade zugewiesen wird (standardmäßig 0–0,5).
| `PrimaryCandleType` | Arbeitszeitrahmen für die Ausbruchserkennung (standardmäßig 15-Minuten-Kerzen).
| `SecondaryCandleType` | Schnellerer Zeitrahmen, der die Nachlaufdistanz steuert (standardmäßige 1-Minuten-Kerzen).
| `TakeMultiplier` | Multiplikator des primären Bereichs, der zum Platzieren der Take-Profit-Order verwendet wird.
| `StopMultiplier` | Multiplikator des primären Bereichs, der zur Berechnung des Schutzstopps verwendet wird.
| `TrailMultiplier` | Multiplikator des sekundären Bereichs, der die Nachlaufdistanz definiert.
| `BaseVolume` | Größe der Ersatzposition, wenn Portfoliokennzahlen nicht verfügbar sind.
| `StopBufferTicks` | Zusätzlicher Abstand in Preisticks, der zwischen Einstiegs- und Stop-/Zielniveau bleiben muss.
| `FreezeBufferTicks` | Zusätzlicher Puffer, der verhindert, dass Stoppaktualisierungen zu nahe am Einfrierniveau des Brokers liegen.
| `TrailStepMultiplier` | Spread-Multiplikator, der das minimale Inkrement zwischen nachfolgenden Anpassungen definiert.

## Nutzungshinweise
- Füttern Sie die Strategie mit beiden Kerzenserien und einem Level-1- oder Orderbuch-Stream, damit die besten Geld-/Briefkurse verfügbar sind. Laufen
Wenn nur die Daten des letzten Handels berücksichtigt werden, ändern sich die Ausbruchsprüfungen, da sie auf dem Spread basieren.
- Die Standardparameterwerte replizieren das MQ4-Vorlagenbeispiel (`TakeMultiplier = 1`, `StopMultiplier = 2`,
`TrailMultiplier = 3`). Passen Sie sie an die Vermögenswerte an, mit denen Sie handeln, oder experimentieren Sie mit der Ausbruchsintensität.
- Der Trailing Stop ist virtuell – Aufträge werden an der Börse nicht geändert. Wenn die Nachstellbedingung erfüllt ist, ist die Strategie einfach
gibt einen Marktausstieg aus, der die Art und Weise widerspiegelt, wie der ursprüngliche Fachberater die Stopps intern verwaltet hat.
- Kombinieren Sie die Strategie mit dem integrierten Schutzmodul von StockSharp (bereits im Konstruktor aktiviert), um den Notfall aufrechtzuerhalten
Stop-Loss-Handhabung, auch wenn die Strategie vorübergehend unterbrochen ist.
