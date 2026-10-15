# Malr Kanalausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche eines benutzerdefinierten MALR (Moving Average Linear Regression)-Kanals. Der MALR-Indikator kombiniert einen einfachen gleitenden Durchschnitt und einen linear gewichteten gleitenden Durchschnitt, um eine Mittellinie zu bilden. Die Standardabweichung des Preises relativ zu dieser Linie erzeugt zwei äußere Bänder.

Eine Long-Position wird eröffnet, wenn das obere Ausbruchsband die Schlusskurslinie von oben nach unten kreuzt, was auf einen Aufwärtsausbruch hindeutet. Eine Short-Position wird eröffnet, wenn das untere Ausbruchsband den Schlusskurs von unten nach oben kreuzt, was einen Abwärtsausbruch signalisiert.

## Parameter

- `MaPeriod` – Zeitraum für die gleitenden Durchschnitte und die Standardabweichung.
- `ChannelReversal` – Breite des inneren MALR-Kanals gemessen in Standardabweichungen.
- `ChannelBreakout` – zusätzliche Breite für den äußeren Ausbruchskanal.
- `CandleType` – Kerzentyp, der für Berechnungen verwendet wird.

## Funktionsweise

1. SMA und LWMA des Schlusskurses berechnen.
2. Die MALR-Linie `FF = 3 * LWMA - 2 * SMA` berechnen.
3. Standardabweichung von `close - FF` über denselben Zeitraum messen.
4. Ausbruchsbänder ableiten: `FF ± StdDev * (ChannelReversal + ChannelBreakout)`.
5. Long eingehen, wenn das obere Band von oben nach unten unter den Schlusskurs kreuzt.
6. Short eingehen, wenn das untere Band von unten nach oben über den Schlusskurs kreuzt.

Die Strategie schließt immer die entgegengesetzte Position, bevor sie eine neue eröffnet.
