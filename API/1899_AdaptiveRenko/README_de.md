# Adaptive Renko-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt ein adaptives Renko-Raster, bei dem die Ziegelgröße der Marktvolatilität folgt, die durch den **Average True Range (ATR)**-Indikator gemessen wird. Ein Trade wird ausgeführt, wenn der Preis einen vollständigen Ziegel in eine Richtung zurücklegt.

## Logik
- ATR wird über einen konfigurierbaren `VolatilityPeriod` berechnet.
- Die Ziegelgröße entspricht `ATR * Multiplier`, darf jedoch nicht kleiner als `MinBrickSize` sein.
- Wenn der Preis um mindestens eine Ziegelgröße über den vorherigen Ziegel steigt, kauft die Strategie (und schließt dabei Short-Positionen, falls vorhanden).
- Wenn der Preis um mindestens eine Ziegelgröße unter den vorherigen Ziegel fällt, verkauft die Strategie (und schließt dabei Long-Positionen, falls vorhanden).

## Parameter
- `Volume` – Auftragsvolumen.
- `VolatilityPeriod` – Zeitraum für den ATR.
- `Multiplier` – Koeffizient, der auf den ATR angewendet wird.
- `MinBrickSize` – minimale zulässige Ziegelgröße in Preiseinheiten.
- `CandleType` – Zeitrahmen für die ATR-Berechnung.

## Zeitrahmen
- Standard: 4 Stunden.
