# Bulls & Bears Power Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis der Kreuzung der Indikatoren Bulls Power und Bears Power auf einem Vier-Stunden-Zeitrahmen. Bulls Power misst den Kaufdruck oberhalb eines Durchschnittspreises, während Bears Power den Verkaufsdruck darunter zeigt. Wenn die Kaufstärke die Verkaufsstärke übersteigt, eröffnet das System eine Long-Position. Wenn die Verkaufsstärke dominant wird, eröffnet es eine Short-Position.

Tests mit historischen Kryptodaten zeigen, dass klare Kreuzungen oft kurzfristigen Umkehrungen vorausgehen. Die Strategie ist darauf ausgelegt, immer entweder long oder short zu sein und die Position bei jeder Kreuzung in die entgegengesetzte Richtung umzukehren.

## Details

- **Einstiegskriterien**:
  - **Long**: Bulls Power-Wert kreuzt über Bears Power.
  - **Short**: Bears Power-Wert kreuzt über Bulls Power.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung, die die Position umkehrt.
- **Stops**: Keine. Positionen werden umgekehrt statt ausgestoppt.
- **Filter**:
  - Zeitrahmen: standardmäßig 4-Stunden-Kerzen.
  - Indikatoren: Bulls Power, Bears Power.
  - Richtung: Umkehr basierend auf Momentum-Wechsel.
