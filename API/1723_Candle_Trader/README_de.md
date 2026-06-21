# Candle Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Candle Trader-Strategie** analysiert die Richtung (bullisch oder bärisch) der letzten vier abgeschlossenen Kerzen, um kurzfristige Umkehrmöglichkeiten zu identifizieren. Sie operiert auf einem einzelnen Instrument und sendet Marktorders mit vordefinierten Take-Profit- und Stop-Loss-Niveaus.

## Strategielogik

1. **Long-Einstieg (direkt)** – letzte Kerze bullisch, die beiden vorherigen bärisch.
2. **Long-Einstieg (Fortsetzung)** – letzte Kerze bullisch, vorherige Kerze bärisch, die zwei Kerzen davor bullisch. Diese Regel ist nur aktiv, wenn *Continuation* `true` ist.
3. **Short-Einstieg (direkt)** – letzte Kerze bärisch, die beiden vorherigen bullisch.
4. **Short-Einstieg (Fortsetzung)** – letzte Kerze bärisch, vorherige Kerze bullisch, die zwei Kerzen davor bärisch. Nur aktiv, wenn *Continuation* `true` ist.
5. Wenn *Reverse Close* aktiviert ist und ein neues Signal entgegen der aktuellen Position erscheint, schließt die Strategie die bestehende Position, bevor sie eine neue eröffnet.
6. Alle Orders werden durch feste Take-Profit- und Stop-Loss-Werte in Preisschritten geschützt.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `Volume` | Auftragsvolumen für jeden Trade. |
| `TakeProfitTicks` | Take-Profit-Distanz in Preisschritten. |
| `StopLossTicks` | Stop-Loss-Distanz in Preisschritten. |
| `Continuation` | Aktiviert die Fortsetzungsmuster für zusätzliche Einstiege. |
| `ReverseClose` | Schließt eine offene Position, bevor die Gegenrichtung eingegangen wird. |
| `CandleType` | Kerzen-Zeitrahmen für die Analyse. |

## Hinweise

- Die Strategie wertet nur abgeschlossene Kerzen aus.
- Sie verwendet Marktorders und storniert aktive Orders, bevor neue gesendet werden.
- Stop-Loss- und Take-Profit-Niveaus werden über `StartProtection` angewendet.
- Die Positionsgröße kann über den Parameter `Volume` optimiert werden.
