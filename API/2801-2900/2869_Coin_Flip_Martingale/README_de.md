# Münzwurf-Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Münzwurf-Strategie emuliert den ursprünglichen MetaTrader-Expertenberater, bei dem Einstiege durch einen pseudozufälligen Münzwurf bestimmt werden. Es kann jeweils nur eine Position eröffnet werden. Jede abgeschlossene Kerze dient als Entscheidungspunkt: Wenn der vorherige Trade glatt ist, wird eine Münze geworfen und sofort eine Long- oder Short-Position mit dem berechneten Handelsvolumen eröffnet. Jeder Trade ist mit Stop-Loss- und Take-Profit-Levels geschützt, während ein optionaler Trailing Stop das Risiko enger zieht, wenn sich der Markt zugunsten der Position bewegt.

Ein Martingale-artiges Positionsgrößenmodell wird implementiert. Wenn die vorherige Position gestoppt wurde, erhöht der nächste Trade seine Größe um einen konfigurierbaren Multiplikator. Erfolgreiche Trades setzen das Volumen auf die Basisgröße zurück. Ein benutzerdefiniertes maximales Volumen verhindert unkontrolliertes Wachstum der Trade-Größe.

## Handelsregeln

1. Bei jeder abgeschlossenen Kerze bewertet die Strategie die aktuelle Position.
2. Wenn keine Position offen ist, wählt eine pseudozufällige Zahl die Long- oder Short-Richtung. Beide Seiten sind mit gleicher Wahrscheinlichkeit erlaubt.
3. Jeder neue Trade verwendet das Basisvolumen, es sei denn, der vorherige Trade endete mit einem Stop-Loss. In diesem Fall wird das Volumen mit dem Martingale-Faktor multipliziert, wobei das maximale Volumenlimit eingehalten wird.
4. Schutz-Stop-Loss- und Take-Profit-Preise werden an jede Position angehängt. Wenn der Schlusskurs diese Schwellen erreicht, wird die Position mit einer Marktorder geschlossen.
5. Der Trailing Stop überwacht die günstige Bewegung. Sobald der Gewinn die Trailing-Distanz plus Schritt übersteigt, wird das Stop-Level in Richtung des Preises bewegt, um Gewinne zu sichern.

## Parameter

- **Stop Loss** – Abstand in Preisschritten zur Berechnung des Stop-Loss vom Einstandspreis.
- **Take Profit** – Abstand in Preisschritten zum Einstandspreis für den Take-Profit.
- **Trailing Stop** – Gewinnabstand, der den Trailing-Stop-Mechanismus aktiviert. Auf null setzen, um das Trailing zu deaktivieren.
- **Trailing Step** – Mindestgewinn, der erforderlich ist, bevor der Trailing Stop erneut bewegt wird.
- **Base Volume** – Volumen des ersten Trades in einem Martingale-Zyklus.
- **Martingale Mult** – Multiplikator, der auf das zuletzt gestoppte Volumen angewendet wird, um die nächste Ordergröße zu bestimmen.
- **Max Volume** – Harte Obergrenze für die Ordergröße. Wenn überschritten, wird der Trade übersprungen und eine Warnung protokolliert.
- **Candle Type** – Kerzenserie, die definiert, wann Münzwürfe und Risikomanagement-Überprüfungen ausgeführt werden.

## Hinweise

- Die Strategie verwendet Marktorders sowohl für Einstiege als auch für Ausstiege, um das Verhalten des ursprünglichen Expertenberaters nachzuahmen.
- Trailing-Stop-Berechnungen basieren auf dem Preisschritt des Instruments. Wenn kein Preisschritt verfügbar ist, werden stattdessen rohe Punktwerte verwendet.
- Zufallszahlen werden mit einem deterministischen Seed basierend auf der aktuellen Zeit generiert, um identische Sequenzen bei gleichzeitigen Läufen zu vermeiden.
