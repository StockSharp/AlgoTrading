# Vortex-Kreuzungs-Strategie mit MA-Bestätigung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Vortex-Indikator zur Erkennung von Trendwenden und bestätigt Einstiege mit einem geglätteten gleitenden Durchschnitt. Ein Long-Trade wird eröffnet, wenn der positive Vortex den negativen nach oben kreuzt und der Preis über der Glättungslinie liegt. Ein Short-Trade erfolgt beim umgekehrten Kreuzungssignal unterhalb der Linie.

## Parameter
- **Vortex Length** – Zeitraum für die Vortex-Berechnung.
- **SMA Length** – Länge der Basis-SMA.
- **Smoothing Length** – Länge des glättenden gleitenden Durchschnitts.
- **MA Type** – Glättungsmethode.
- **Candle Type** – Zeitrahmen der verarbeiteten Kerzen.
