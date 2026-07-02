# Cross MA ATR Benachrichtigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4 „CrossMA“-Expertenberaters. Es handelt den Schnittpunkt zwischen zwei einfachen gleitenden Durchschnitten und schützt jeden Handel mit einem auf dem Average True Range (ATR) basierenden Stop-Loss. Zusätzlich zur ursprünglichen Logik erfasst die Strategie detaillierte Informationsnachrichten, anstatt E-Mails zu versenden.

## Handelslogik
1. Die Strategie abonniert die konfigurierte Kerzenserie und berechnet einen schnellen und einen langsamen einfachen gleitenden Durchschnitt zusammen mit einem ATR-Indikator.
2. Wenn der schnelle SMA den langsamen SMA überschreitet, wird jede Short-Position geschlossen und eine Long-Position eröffnet. Der Stop-Loss wird einen ATR unter dem Einstiegspreis platziert.
3. Wenn der schnelle SMA den langsamen SMA unterschreitet, wird jede Long-Position geschlossen und eine Short-Position eröffnet. Der Stop-Loss wird einen ATR über dem Einstiegspreis platziert.
4. Bei jeder fertigen Kerze wird der Stop-Preis überprüft. Wenn der Preis das Stop-Level erreicht, wird die Position sofort zum Marktwert geschlossen.

## Risikomanagement
- Die Positionsgröße wird aus dem Kontokapital und dem Parameter `Maximum Risk` berechnet. Wenn keine Aktieninformationen verfügbar sind, fällt die Strategie auf den Wert `Base Volume` zurück.
- Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird die Positionsgröße proportional zum `Decrease Factor` reduziert, wodurch das ursprüngliche MetaTrader-Verhalten reproduziert wird.
- Alle Volumina werden auf den Sicherheitsvolumenschritt normalisiert, um gültige Bestellgrößen sicherzustellen.

## Benachrichtigungen
Anstatt E-Mails zu versenden, schreibt die Strategie klare Protokollnachrichten, wann immer Aufträge durch Signale oder Stopps geöffnet oder geschlossen werden. Die Protokollierung kann über den Parameter `Enable Notifications` deaktiviert werden.

## Parameter
- **Kerzentyp** – Kerzentyp, der für Indikatorberechnungen verwendet wird.
- **Fast SMA Period** – Periode des schnellen gleitenden Durchschnitts (Standard 4).
- **Slow SMA Period** – Periode des langsamen gleitenden Durchschnitts (Standard 12).
- **ATR Zeitraum** – Anzahl der Kerzen, die von ATR für die Stop-Berechnung verwendet werden (Standard 6).
- **Basisvolumen** – Mindesthandelsvolumen, wenn eine risikobasierte Größenbestimmung nicht verfügbar ist (Standard 0,1).
- **Maximales Risiko** – Anteil des Eigenkapitals, der jedem Trade zugewiesen wird (Standard 0,02).
- **Verringerungsfaktor** – reduziert die Positionsgröße nach verlorenen Trades (Standard 3).
- **Benachrichtigungen aktivieren** – ermöglicht die Protokollierung von Handelsaktionen.
