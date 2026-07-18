# Trailing-Stop-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwaltet eine bestehende Position durch die Anwendung eines Trailing Stops. Sie überwacht Tick-Trades und verschiebt das Stop-Niveau, wenn sich der Preis in eine günstige Richtung bewegt. Wenn der Markt sich umkehrt und das Trailing-Niveau erreicht, schließt die Strategie die Position.

## Details

- **Einstieg**: Die Strategie eröffnet keine Positionen; es wird davon ausgegangen, dass eine Position bereits offen ist.
- **Long-Logik**: Bei Long-Positionen folgt der Stop dem Preis nach oben, sobald der Preis um die Trailing-Distanz gestiegen ist.
- **Short-Logik**: Bei Short-Positionen bewegt sich der Stop nach unten, wenn der Preis fällt.
- **Ausstieg**: Die Position wird geschlossen, wenn der Preis den Trailing Stop erreicht.
- **Indikatoren**: Keine.
- **Zeitrahmen**: Tick-basiert, reagiert auf jeden Trade.
- **Stops**: Nur Trailing Stop.

## Parameter

- `TrailingPoints` — Trailing-Distanz in Punkten (Preisschritte). Standard: 200.
