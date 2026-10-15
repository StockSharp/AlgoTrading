# Kanal-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen linearen Regressionskanal zur Signalgenerierung.

Es werden drei Handelsregeln unterstützt:

1. Handel mit dem Trend – Einstieg bei Steigungsänderungen.
2. Kanal-Ausbrüche handeln – Einstieg bei Ausbrüchen über oder unter den Kanal.
3. Innerhalb des Kanals handeln – Gegenbewegungen im Kanal mit optionalem Trendfilter ausnutzen.

Zu den Parametern gehören Periode, Zonenprozentsatz, Bereichsquelle, Handelsregel, Trendfilter und eine Nur-Long-Option.
