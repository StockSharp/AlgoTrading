# Binario 3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Experten „Binario_3“ von `MQL/7658/Binario_3.mq4`. Der ursprüngliche EA umgibt den Markt mit zwei exponentiellen gleitenden Durchschnitten über 144 Perioden, die anhand der Kerzenhochs und -tiefs berechnet werden, und handelt mit dem Ausbruch dieses adaptiven Kanals. Ausstehende Stop-Orders werden oberhalb des oberen Bandes und unterhalb des unteren Bandes platziert, während Schutzstopps, Take-Profit-Ziele und ein optionaler Trailing-Stop das MetaTrader-Verhalten nachahmen.

Die StockSharp-Version behält die gleichen Entscheidungsregeln bei, wird jedoch mit dem High-Level-API implementiert:

1. Abonniert die konfigurierte Kerzenserie und berechnet die beiden EMA-Umschläge jedes Mal neu, wenn eine Kerze abgeschlossen ist.
2. Wenn der letzte Schlusskurs innerhalb des Kanals bleibt, werden Kauf-Stopp- und Verkaufsstopp-Orders mit dem erforderlichen Offset von den EMA-Werten platziert.
3. Zeichnet die mit jeder ausstehenden Order verbundenen Stop-Loss- und Take-Profit-Level auf, sodass sie auf die Position angewendet werden können, sobald eine Order ausgeführt wird.
4. Verfolgt Level-1-Kurse zur Verwaltung offener Positionen: Schließt Geschäfte, wenn der Preis den aufgezeichneten Stop-Loss, das Ziel oder die Trailing-Stop-Distanz erreicht.
5. Storniert ausstehende Aufträge, wenn der Preis den Kanal verlässt oder wenn die entgegengesetzte Position aktiv wird, und spiegelt die Bereinigungslogik im MQL-Skript wider.

## Parameter

| Name | Standard | Beschreibung |
|------|---------|-------------|
| `TakeProfit` | `850` Punkte | Zusätzlicher Abstand (in Punkten), der bei der Berechnung des Take-Profits zur Ausbruchsseite hinzugefügt wird. |
| `TrailingStop` | `850` Punkte | Entfernung in Punkten, die für nachlaufende Ausfahrten verwendet wird. Auf `0` setzen, um das Nachstellen zu deaktivieren. |
| `PipDifference` | `25` Punkte | Offset vom EMA-Kanal vor der Platzierung ausstehender Bestellungen. |
| `Lots` | `0.1` | Basishandelsvolumen, das verwendet wird, wenn eine risikobasierte Größenbestimmung nicht abgeleitet werden kann. |
| `MaximumRisk` | `10` | Risikomultiplikator vom Original EA kopiert. Die Strategie schätzt das Volumen auf `max(Lots, Balance * MaximumRisk / 50000)`. |
| `EmaPeriod` | `144` | Zeitraum der exponentiellen gleitenden Durchschnitte, die auf hohen und niedrigen Preisen basieren. |
| `CandleType` | `1 hour` Zeitrahmen | Kerzenserie, die die Aktualisierung von Indikatoren und die Auftragserteilung vorantreibt. |

Alle Punkte werden mithilfe des `PriceStep` des Instruments in tatsächliche Preisabstände umgerechnet. Wenn das Symbol keinen Schritt offenlegt, greift die Strategie auf `1` zurück.

## Handelslogik

1. **Indikatorberechnungen** – Zwei `ExponentialMovingAverage`-Instanzen verarbeiten die Höchst- und Tiefstpreise der Kerze. Aufträge werden erst generiert, nachdem beide Durchschnittswerte vollständig gebildet sind.
2. **Ausstehende Orders** – Wenn der Schlusskurs innerhalb des Kanals liegt, werden Kauf-Stopp- und Verkaufs-Stopp-Orders platziert bei:
   - Kaufstopp: EMA(hoch) + Spread + `PipDifference` * Schritt.
   - Verkaufsstopp: EMA(niedrig) – `PipDifference` * Schritt.
Die mit diesen Aufträgen verbundenen Stop-Loss- und Take-Profit-Werte werden gespeichert, bis die Position aktiv wird.
3. **Positionsverwaltung** – Sobald eine Position eröffnet wird, storniert die Strategie die entgegengesetzte ausstehende Order und übernimmt die gespeicherten Stop-/Zielniveaus. Level-1-Kurse werden überwacht, um den Handel zu schließen, wenn der Markt den Stop-Loss, Take-Profit oder die Trailing-Stop-Distanz (`TrailingStop` * Schritt) erreicht.
4. **Trailing Stop** – Bei Long-Positionen folgt das Trailing-Level dem besten Gebot, sobald der Gewinn die konfigurierte Distanz überschreitet; Bei Shorts richtet sich das Niveau nach der besten Nachfrage. Das nachgestellte Niveau bewegt sich nur in Richtung des Handels und reproduziert das nachgestellte Verhalten von MetaTrader.
5. **Auftragsbereinigung** – Wenn der letzte Abschluss den EMA-Kanal verlässt, werden beide ausstehenden Aufträge storniert, um unerwünschte Eingaben zu vermeiden, was den Sicherheitsprüfungen des ursprünglichen Skripts entspricht.

## Unterschiede zur MQL-Version

- Die ursprünglichen EA modifizierten serverseitigen Stop-Orders mit `OrderModify`; Der Port StockSharp simuliert den gleichen Effekt, indem er Kurse der Stufe 1 beobachtet und `ClosePosition()` ruft, wenn ein Stopp oder Ziel erreicht wird.
- Trailing-Stops werden vollständig innerhalb der Strategie implementiert, da High-Level-Orders vom Typ StockSharp keine Trailing-Anweisungen an der Börse unterstützen.
- Bei der Volumenberechnung wird der Portfoliosaldo (`Portfolio.CurrentValue` oder `Portfolio.BeginValue`) verwendet, sofern verfügbar. Wenn der Saldo nicht bekannt ist, greift die Strategie auf den konfigurierten `Lots`-Wert zurück.
- Die Preise werden vor der Registrierung von Aufträgen auf die Preisstufe des Instruments normalisiert, um sie an die Börsenanforderungen anzupassen.

## Nutzungshinweise

- Aktivieren Sie Level-1-Abonnements, wenn Sie die Strategie ausführen, damit Trailing Stops und Protective Exits auf Live-Bid/Ask-Updates reagieren können.
- Die Strategie basiert auf abgeschlossenen Kerzen. Wenn der ausgewählte Zeitrahmen zu groß ist, spiegelt die Reaktionszeit diesen langsameren Rhythmus wider.
- Trailing kann deaktiviert werden, indem `TrailingStop` auf `0` gesetzt wird. In diesem Modus werden nur die festen Stop-Loss- und Take-Profit-Level verwendet.
