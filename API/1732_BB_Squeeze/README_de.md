# BB-Squeeze-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie überwacht die Kontraktion und Expansion der Bollinger-Bänder, um Volatilitätsausbrüche zu nutzen. Ein Squeeze wird definiert als ein Zeitraum, in dem der Abstand zwischen dem oberen und unteren Bollinger-Band im Verhältnis zur mittleren Linie eng wird. Sobald die Volatilität zunimmt und der Preis nach einem Squeeze außerhalb des Bandes schließt, tritt das System in Richtung des Ausbruchs ein.

Positionen werden mit Marktaufträgen eröffnet. Eine Long-Position wird erstellt, wenn der Preis nach einem Squeeze über dem oberen Band schließt, während eine Short-Position eröffnet wird, wenn der Preis unter dem unteren Band schließt. Es werden nur abgeschlossene Kerzen verarbeitet, um vorzeitige Signale während der Bildung zu vermeiden.

Der Algorithmus verfolgt Änderungen der Bandbreite ohne vollständige Kerzenhistorien zu speichern. Durch den Vergleich der aktuellen Breite mit der vorherigen stellt er sicher, dass eine echte Expansion stattfindet, bevor Aufträge platziert werden. Dies vermeidet Einstiege in längere Niedrigvolatilitätsphasen, in denen sich kein Ausbruch entwickelt.

Die Standardparameter verwenden ein 20-Perioden-Bollinger-Band mit einem Breitenmultiplikator von 2. Der Squeeze-Schwellenwert ist auf 0.05 gesetzt, was bedeutet, dass die Bänder innerhalb von fünf Prozent der mittleren Linie liegen müssen, um geringe Volatilität zu registrieren. Der Kerzen-Zeitrahmen und alle numerischen Werte sind vollständig konfigurierbar und unterstützen die Optimierung in der StockSharp-Umgebung.
