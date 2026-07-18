# Strategie des Bereichserweiterungs-Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Tom DeMarks **Range Expansion Index (REI)**, um die Stärke und Schwäche des Preises zu bewerten. Der Indikator vergleicht aktuelle Hochs und Tiefs mit früheren Preisen und oszilliert zwischen positiven und negativen Werten.

## Funktionsweise

- Wenn der REI nach einem Aufenthalt darunter über den **Unteren Level** (Standard `-60`) steigt, eröffnet die Strategie eine **Long**-Position.
- Wenn der REI nach einem Aufenthalt darüber unter den **Oberen Level** (Standard `60`) fällt, eröffnet die Strategie eine **Short**-Position.
- Entgegengesetzte Positionen werden automatisch geschlossen, wenn ein entgegengesetztes Signal auftritt.

## Parameter

- `REI Period` – Anzahl der Balken, die für die REI-Berechnung verwendet werden (Standard `8`).
- `Up Level` – oberer Schwellenwert, der bei einem Abwärtsdurchbruch Kursschwäche anzeigt (Standard `60`).
- `Down Level` – unterer Schwellenwert, der bei einem Aufwärtsdurchbruch Kursstärke anzeigt (Standard `-60`).
- `Candle Type` – Zeitrahmen der Kerzen für die Indikatorberechnung (Standard `8 Stunden`).

## Verwendung

Hängen Sie die Strategie an ein Wertpapier und starten Sie sie. Die Strategie abonniert die angegebene Kerzenserie und verwendet Marktaufträge, um Positionen basierend auf REI-Signalen zu eröffnen oder zu schließen.
