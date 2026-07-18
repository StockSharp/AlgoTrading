# McClellan A-D Volumen-Integrationsmodell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt eine gewichtete Advance-Decline-Linie, indem die Kursspanne des Balkens mit seinem Volumen multipliziert wird. Zwei EMAs dieser gewichteten Linie bilden einen McClellan-ähnlichen Oszillator.

Eine Long-Position wird eröffnet, wenn der Oszillator nach einem Aufenthalt darunter einen benutzerdefinierten Schwellenwert von unten kreuzt. Der Trade wird automatisch nach einer festen Anzahl von Balken geschlossen.

## Details

- **Einstieg**: Oszillator kreuzt `Long Entry Threshold` von unten nach oben.
- **Ausstieg**: Position nach `Exit After Bars` Kerzen geschlossen.
- **Long/Short**: Nur Long.
- **Indikatoren**: Zwei EMAs.
- **Stops**: Keine.
- **Zeitrahmen**: Konfigurierbar.
