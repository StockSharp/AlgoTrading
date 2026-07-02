# MartingaleEA-5-Level-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **MartingaleEA-5 Levels-Strategie** ist eine direkte Portierung des MetaTrader 5 Expertenberaters „MartingaleEA-5 Levels“ auf das StockSharp High-Level API. Das System überwacht eine bestehende Position und erstellt ein fünfstufiges Durchschnittsraster, wann immer sich der Markt dagegen bewegt. Die gesamte Logik läuft auf fertigen Kerzen ab, wodurch das Verhalten sowohl in historischen Tests als auch im Live-Handel reproduzierbar bleibt.

## Handelslogik

1. **Überwachung des bestehenden Engagements** – Die Strategie geht davon aus, dass eine anfängliche Long- oder Short-Position vorhanden ist. Sie können den ersten Handel manuell oder über eine andere Strategie eröffnen.
2. **Erkennung unerwünschter Bewegungen** – Bei jeder abgeschlossenen Kerze misst die Strategie, wie weit sich der aktuelle Preis vom Eintrag mit dem schlechtesten Preis der aktiven Gruppe (höchster Long oder niedrigster Short) entfernt hat.
3. **Martingale Ergänzungen** – wenn der gleitende Verlust der Gruppe negativ ist und die Gegenbewegung die konfigurierten kumulativen Distanzen überschreitet, sendet die Strategie zusätzliche Marktaufträge. Jede weitere Bestellung multipliziert die vorherige mit `VolumeMultiplier`. Es können bis zu fünf Stufen konfiguriert werden; Der Parameter `MaxAdditions` begrenzt, wie viele davon tatsächlich verwendet werden.
4. **Gewinn- und Verlustzielsetzung** – während eine Gruppe geöffnet ist, summiert die Strategie kontinuierlich den nicht realisierten Gewinn- und Verlustgewinn für diese Richtung. Sobald die Gesamtsumme `TakeProfitCurrency` erreicht oder unter `StopLossCurrency` fällt, werden alle Orders auf dieser Seite mit einer Marktorder geschlossen und die Martingalzähler werden zurückgesetzt.
5. **Volumennormalisierung** – jedes Auftragsvolumen durchläuft die `VolumeStep`, `MinVolume` und `MaxVolume` des Instruments, um das Senden nicht ausführbarer Mengen zu vermeiden.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `EnableMartingale` | Schaltet die Mittelungs- und Liquidationslogik ein oder aus. | `true` |
| `VolumeMultiplier` | Faktor, der beim Hinzufügen einer neuen Ebene auf das vorherige Auftragsvolumen angewendet wird. | `2.0` |
| `MaxAdditions` | Maximale Anzahl Martingalschritte pro Richtung (bis zu fünf). | `4` |
| `Level1DistancePips` | Anfängliche negative Distanz (in Pips) vor der Eröffnung der zweiten Order. | `300` |
| `Level2DistancePips` | Für die dritte Ordnung ist zusätzlicher Abstand erforderlich. | `400` |
| `Level3DistancePips` | Für die vierte Ordnung ist zusätzlicher Abstand erforderlich. | `500` |
| `Level4DistancePips` | Für die fünfte Ordnung ist zusätzlicher Abstand erforderlich. | `600` |
| `Level5DistancePips` | Zusätzlicher Abstand für die sechste Ordnung erforderlich (falls zulässig). | `700` |
| `TakeProfitCurrency` | Nicht realisierter Gewinn (Kontowährung), der die gesamte Gruppe schließt. | `200` |
| `StopLossCurrency` | Nicht realisierter Verlust (Kontowährung), der einen Notausgang erzwingt. | `-500` |
| `CandleType` | Für Auswertungen verwendeter Zeitrahmen (Standard-1-Minuten-Kerzen). | `TimeFrame(1m)` |

> **Pip-Umrechnung** – jede Distanz wird mit dem Preisschritt des Instruments (`PriceStep` oder `MinPriceStep`) multipliziert. Für Symbole, die in gebrochenen Pips angegeben werden, passen Sie die Werte entsprechend an.

## Hinweise und Empfehlungen

- Die Implementierung spiegelt die ursprüngliche EA wider, einschließlich der Annahme, dass jeweils nur ein Richtungskorb aktiv ist. Das gleichzeitige Öffnen von Positionen in beide Richtungen führt dazu, dass jede Seite unabhängig verwaltet wird.
- Da die Strategie nur auf Kerzenschließungen reagiert, wählen Sie einen Zeitrahmen, der der gewünschten Reaktionsfähigkeit entspricht. Kürzere Zeitrahmen ahmen das Verhalten auf Tick-Ebene besser nach.
- Martingale-Techniken erhöhen das Risiko. Testen Sie immer mit realistischen Slippage- und Provisionsmodellen und definieren Sie konservative Stop-Levels, bevor Sie die Strategie auf Live-Märkten aktivieren.
- Die Strategie erstellt noch keinen Python-Port. Wie gewünscht ist nur die C#-High-Level-Implementierung enthalten.
