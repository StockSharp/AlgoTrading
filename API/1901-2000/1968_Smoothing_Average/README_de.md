# Gleitender-Durchschnitt-Glättungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie handelt rund um einen einfachen gleitenden Durchschnitt (SMA) mit einem zusätzlichen Glättungsversatz. Sie versucht, Preisabweichungen vom gleitenden Durchschnitt zu nutzen, indem sie Positionen eröffnet, wenn der Schlusskurs einen Versatzabstand vom Durchschnitt überschreitet.

## Funktionsweise
- Berechnung eines SMA des gewählten Kerzentyps.
- Wenn keine offene Position vorhanden ist:
  - Short-Position eröffnen, wenn der Schlusskurs unter `SMA + Smoothing` liegt.
  - Long-Position eröffnen, wenn der Schlusskurs über `SMA - Smoothing` liegt.
- Bei einer offenen Short-Position:
  - Position schließen, wenn der Schlusskurs über `SMA + Smoothing` steigt.
- Bei einer offenen Long-Position:
  - Position schließen, wenn der Schlusskurs unter `SMA - Smoothing` fällt.

Die Strategie verwendet Marktaufträge und arbeitet nur mit abgeschlossenen Kerzen.

## Parameter
- **MA Period** – Rückblickperiode für den SMA.
- **Smoothing** – Preisversatz, der beim Generieren von Signalen zum SMA addiert oder subtrahiert wird.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Hinweise
Diese Konvertierung basiert auf dem originalen MQL4-Skript `smoothingaverage.mq4`.
