# Very Blonde System Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gitterbasierte Gegentrend-Strategie, inspiriert vom originalen "Very Blonde System" für MetaTrader. Die Strategie sucht nach einem großen Abstand zwischen dem aktuellen Preis und jüngsten Extremwerten und handelt in die entgegengesetzte Richtung.

## Strategielogik
1. Den höchsten Hochpunkt und niedrigsten Tiefpunkt über die letzten *Count Bars* Kerzen berechnen.
2. Wenn keine offenen Positionen vorhanden sind:
   - Wenn der Abstand vom jüngsten Hoch zum aktuellen Preis *Limit* Ticks übersteigt, zum Marktpreis kaufen.
   - Wenn der Abstand vom aktuellen Preis zum jüngsten Tief *Limit* Ticks übersteigt, zum Marktpreis verkaufen.
   - Nach dem Einstieg vier zusätzliche Limitorders alle *Grid* Ticks platzieren, dabei das Volumen auf jeder Stufe verdoppeln.
3. Wenn eine Position vorhanden ist:
   - Wenn der Gesamtgewinn *Amount* Währungseinheiten übersteigt, die Position schließen und alle ausstehenden Orders stornieren.
   - Wenn *Lock Down* größer als null ist, aktiviert die Strategie nach einer Preisbewegung um diese Anzahl Ticks in günstiger Richtung einen Gewinnsicherungsschutz. Kehrt der Preis zum Einstandsniveau zurück, werden alle Positionen geschlossen.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `CountBars` | Anzahl der Kerzen für die Suche nach Hochs und Tiefs. |
| `Limit` | Mindestabstand vom Extremwert in Ticks zum Öffnen eines Trades. |
| `Grid` | Abstand in Ticks zwischen zusätzlichen Gitterorders. |
| `Amount` | Gewinnziel in Währung zum Schließen aller Positionen. |
| `LockDown` | Abstand in Ticks zum Aktivieren des Gewinnsicherungsschutzes. |
| `CandleType` | Kerzentyp für Berechnungen. |

Die Strategie verwendet Marktorders für initiale Einstiege und Limitorders für Gitterstufen. Alle Kommentare im Code sind auf Englisch verfasst.
