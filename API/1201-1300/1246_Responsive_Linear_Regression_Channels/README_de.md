# Adaptive Lineare Regressions-Kanäle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptiver linearer Regressionskanal, der den Lookback an den Zeitrahmen anpasst und Rücksetzer handelt.

## Details

- **Daten**: Preiskerzen.
- **Einstieg**: Kaufen, wenn der Preis im Aufwärtstrend unter das untere Band fällt; verkaufen, wenn der Preis im Abwärtstrend über das obere Band steigt.
- **Ausstieg**: Schließen, wenn der Preis zur Regressionslinie zurückkehrt.
- **Instrumente**: Beliebig.
- **Risiko**: Die Kanalbreite steuert das Exposure.
