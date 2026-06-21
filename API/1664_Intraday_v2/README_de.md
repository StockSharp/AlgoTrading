# Intraday v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen Intraday-Mean-Reversion-Ansatz mit zwei Sätzen von Bollinger Bändern. Die äußeren Bänder (Abweichung 2.4) definieren Einstiegszonen, während die inneren Bänder (Abweichung 1) die Ausstiege verwalten. Optionale Stop-Loss- und Take-Profit-Levels schließen Positionen, wenn sich der Preis um einen konfigurierbaren Betrag gegen den Trade bewegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs fällt unter das untere äußere Band.
  - **Short**: Der Schlusskurs steigt über das obere äußere Band.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Long: Der Preis kreuzt das untere innere Band nach oben oder trifft Stop-Loss/Take-Profit.
  - Short: Der Preis kreuzt das obere innere Band nach unten oder trifft Stop-Loss/Take-Profit.
- **Stops**: Konfigurierbare absolute Stop-Loss- und Take-Profit-Werte.
- **Filter**: Keine.
