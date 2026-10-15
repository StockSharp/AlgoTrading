# Intraday-Strategie "Out of the Noise" mit VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert den "Out of the Noise" Intraday-Ausbruchsansatz. Die Strategie baut dynamische obere und untere Grenzen um den Sitzungseröffnungskurs herum, unter Verwendung durchschnittlicher absoluter Bewegungen der letzten *Period* Tage.

Long-Positionen werden eröffnet, wenn der Kurs über die obere Grenze ausbricht, während Short-Positionen unterhalb der unteren Grenze eröffnet werden. Bestehende Positionen werden bei einem VWAP-Kreuzung oder beim Berühren der entgegengesetzten Grenze geschlossen. Die Positionsgröße kann optional auf ein Volatilitätsziel skaliert werden, das aus der täglichen Standardabweichung abgeleitet wird.
