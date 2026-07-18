# Volume Weighted MA Digit-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das **Volume Weighted MA Digit System**. Sie erstellt zwei volumengewichtete gleitende Durchschnitte (VWMA) basierend auf den Hoch- und Tiefkursen der Kerzen. Das Kreuzen des Preises durch diese Bänder liefert Handelssignale.

## Funktionsweise

1. **Indikatoren**
   - `VWMA High`: VWMA angewendet auf Kerzenhochs.
   - `VWMA Low`: VWMA angewendet auf Kerzentiefs.
2. **Signale**
   - **Long-Einstieg**: Schlusskurs kreuzt `VWMA High` nach oben.
   - **Short-Einstieg**: Schlusskurs kreuzt `VWMA Low` nach unten.
   - Gegenläufiges Kreuzen schließt offene Positionen.
3. **Risikomanagement**
   - Verwendet integriertes `StartProtection` mit konfigurierbarem Stop-Loss und Take-Profit (Punkte).

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|---------|
| `VwmaPeriod` | VWMA-Berechnungslänge | `12` |
| `CandleType` | Für die Berechnung verwendeter Kerzenzeitrahmen | `4h` |
| `StopLoss` | Stop-Loss in Punkten | `1000` |
| `TakeProfit` | Take-Profit in Punkten | `2000` |

## Hinweise

- Es werden nur geschlossene Kerzen verarbeitet.
- Die Strategie verwendet High-Level-API-Funktionen wie `SubscribeCandles`, `Bind` und Standard-Indikatoren.
- Originale MQL-Strategie: `Exp_Volume_Weighted_MA_Digit_System.mq5`.
