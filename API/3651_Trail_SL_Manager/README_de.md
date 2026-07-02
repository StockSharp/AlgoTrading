# Trail SL Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Trail SL Manager ist eine Utility-Strategie, die das Verhalten des ursprünglichen MetaTrader `trailSL`-Experten reproduziert.
Es eröffnet keine Geschäfte selbst. Stattdessen überwacht es bestehende Positionen und passt deren Schutzstoppniveaus dynamisch an.
Die Logik spiegelt das Quellskript wider: Zuerst wird der Stop gedrückt, um die Gewinnschwelle zu erreichen, sobald der Preis um einen konfigurierbaren Betrag steigt, dann hält ein inkrementeller Trailing-Algorithmus weiterhin Gewinne fest, während der Trend anhält.

## Wie es funktioniert

1. Abonniert den konfigurierten Kerzenstrom, um fertige Balken zu überwachen.
2. Verfolgt den durchschnittlichen Einstiegspreis und die Richtung der aktuellen Position.
3. Wenn sich der Preis um `BreakEvenTriggerPoints` zugunsten des Handels bewegt, wird der Stop auf den Einstiegspreis zuzüglich eines optionalen Offsets verschoben.
4. Nach der Break-Even-Aktivierung oder sofort, sofern zulässig, erhöht die Strategie den Stop alle `TrailStepPoints` um `TrailOffsetPoints`, bis sich der Preis umkehrt und die Position zum Marktwert schließt.

Die abschließenden Regeln werden mit der gleichen punktbasierten Arithmetik wie die MetaTrader-Version berechnet, sodass das Verhalten für Händler, die auf StockSharp migrieren, vertraut bleibt.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `EnableBreakEven` | Ermöglicht die Verschiebung des Stops auf die Gewinnschwelle, sobald der Handel profitabel wird. | `true` |
| `BreakEvenTriggerPoints` | Gewinnentfernung in Punkten, die erforderlich ist, um die Break-Even-Bewegung zu aktivieren. | `20` |
| `BreakEvenOffsetPoints` | Zusätzliche Punkte werden dem Einstiegspreis hinzugefügt, wenn die Gewinnschwelle erreicht wird. | `10` |
| `EnableTrailing` | Schaltet die Trailing-Stop-Logik um. | `true` |
| `TrailAfterBreakEven` | Bei `true` beginnt das Trailing erst nach der Break-Even-Anpassung. | `true` |
| `TrailStartPoints` | Der Mindestgewinn in Punkten vor dem Nachlaufen ist zulässig. | `40` |
| `TrailStepPoints` | Gewinnschritt zwischen nachlaufenden Neuberechnungen. | `10` |
| `TrailOffsetPoints` | Bei jedem nachlaufenden Schritt werden dem Stopp Punkte hinzugefügt. | `10` |
| `InitialStopPoints` | Abstand des anfänglichen Schutzstopps, wenn eine neue Position erscheint. | `200` |
| `CandleType` | Kerzenabonnement zur Überwachung von Preisänderungen. | `1 Minute` |

## Nutzung

1. Hängen Sie die Strategie an eine Umgebung an, in der Einträge durch eine andere Strategie oder manuell generiert werden.
2. Konfigurieren Sie die punktbasierten Schwellenwerte so, dass sie der Symbolvolatilität und den Brokeranforderungen entsprechen.
3. Starten Sie die Strategie, damit fertige Kerzen überwacht und Stopps automatisch angepasst werden können.
4. Beobachten Sie die Diagrammzeichnungen, um zu sehen, wie sich die Stop-Levels mit jedem nachlaufenden Schritt entwickeln.

> **Hinweis:** Die Strategie schließt Positionen mit Marktaufträgen, wenn der simulierte Trailing Stop durchbrochen wird. Fügen Sie börsenspezifischen Schutz hinzu (z. B. echte Stop-Orders), wenn Ihr Workflow dies erfordert.
