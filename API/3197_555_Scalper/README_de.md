# 555 Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die 555 Scalper-Strategie ist ein direkter Port des MetaTrader Expert Advisors "555 Scalper". Sie arbeitet auf jedem primären Zeitrahmen und stützt sich dabei auf Filter höherer Zeitrahmen und monatliche Momentum-Bestätigung. Der Algorithmus kombiniert einen schnellen/langsamen linearen gewichteten gleitenden Durchschnitt (LWMA) Crossover mit einer Momentum-Bestätigung im höheren Zeitrahmen und einem monatlichen MACD-Filter. Die Schutzlogik spiegelt den ursprünglichen EA wider, einschließlich Break-Even-Bewegungen, klassischem Pip-basiertem Trailing, Equity-basierten Notfall-Stops und geldbasierten Ausstiegen.

## Trading-Logik
- **Trendfilter:** Berechnet einen schnellen und einen langsamen LWMA auf dem typischen Preis des Trading-Zeitrahmens. Long-Positionen erfordern, dass der schnelle LWMA über dem langsamen liegt; Short-Positionen erfordern das Gegenteil.
- **Kerzenstruktur:** Validiert, dass die beiden letzten abgeschlossenen Kerzen überlappen (Tief vor zwei Bars unter dem vorherigen Hoch für Longs und umgekehrt für Shorts), um die fraktale Bestätigung des EA anzunähern.
- **Momentum-Filter:** Verwendet einen 14-Perioden Momentum-Indikator, der auf einem höheren Zeitrahmen berechnet wird, der vom Trading-Zeitrahmen abgeleitet ist (z.B. M1 → M15, M5 → M30, M15 → H1 usw.). Ein Trade wird nur gültig, wenn mindestens eine der letzten drei Momentum-Messwerte vom neutralen 100-Level um den konfigurierten Schwellenwert (standardmäßig 0.3) abweicht.
- **MACD-Bestätigung:** Wendet einen monatlichen MACD-Filter (12/26/9) an und kauft nur, wenn die MACD-Hauptlinie über der Signallinie liegt, oder verkauft wenn sie darunter liegt.
- **Positionsgröße:** Startet mit einem Basis-Lot und multipliziert jede zusätzliche Eingabe mit dem konfigurierten Lot-Exponenten, was kontrolliertes Pyramiding bis zur maximalen Anzahl von Trades pro Richtung ermöglicht.

## Risikomanagement
- **Initialer Stop-Loss und Take-Profit:** Jede neue Position erhält einen initialen Stop-Loss und Take-Profit basierend auf MetaTrader-Pip-Abständen.
- **Break-Even-Bewegung:** Wenn der Preis eine konfigurierbare Anzahl von Pips im Gewinn zurücklegt, wird der Stop auf Break-Even plus einem Offset verschoben.
- **Trailing-Stop:** Implementiert die ursprüngliche Pip-Trailing-Logik, indem der Stop mit dem Preis verschoben wird, sobald der Trade im Gewinn läuft.
- **Geldziele:** Optionale Geld- und Prozent-Take-Profits schließen die Position, sobald der schwebende Gewinn die konfigurierten Schwellenwerte erreicht.
- **Geld-Trailing:** Verfolgt den maximalen schwebenden Gewinn und steigt aus, wenn der Gewinn nach Erreichen des Auslöseniveaus um einen konfigurierbaren Betrag zurückgeht.
- **Equity-Stop:** Überwacht das maximale Konto-Equity der Sitzung und liquidiert alle Positionen, wenn der schwebende Drawdown den erlaubten Prozentsatz überschreitet.

## Parameter
- **BaseVolume / LotExponent:** Definieren die anfängliche Trade-Größe und den Multiplikator für zusätzliche Einträge.
- **StopLossSteps / TakeProfitSteps:** Pip-Abstände für Schutzebenen.
- **FastMaPeriod / SlowMaPeriod:** Perioden des schnellen und langsamen LWMA-Trendfilters.
- **Momentum-Schwellenwerte:** Erforderliche Abweichung von 100 für Long- und Short-Setups.
- **MaxTrades:** Maximale Anzahl gestaffelter Einträge pro Richtung.
- **Break-Even- und Trailing-Einstellungen:** Konfiguriert den Pip-basierten Break-Even-Auslöser, Offset und Trailing-Abstand.
- **Geldmanagement:** Aktiviert oder deaktiviert Geld-Take-Profit, Prozent-Take-Profit und Geld-Trailing-Steuerung.
- **Equity-Stop:** Prozentsatz des Drawdowns vom Equity-Peak, der einen globalen Ausstieg auslöst.

## Verwendungshinweise
1. Hängen Sie die Strategie an ein beliebiges Instrument an und wählen Sie den gewünschten Trading-Zeitrahmen über den Parameter `CandleType`.
2. Der Momentum-Feed des höheren Zeitrahmens wird automatisch basierend auf dem primären Zeitrahmen berechnet; stellen Sie sicher, dass historische Daten für beide Zeitrahmen verfügbar sind.
3. Der monatliche MACD-Feed benötigt monatliche Kerzendaten. Stellen Sie beim Testen ausreichend historische Daten bereit, um das MACD-Signal aufzuwärmen.
4. Passen Sie Volumen, Pip-Abstände und Geldmanagement-Schwellenwerte an die Volatilität des Instruments und das Risikoprofil des Kontos an.

Die Strategie reproduziert den Kernentscheidungsprozess des ursprünglichen EA und nutzt dabei StockSharps High-Level-API für Datenabonnements, Indikatorverwaltung und Orderausführung.
