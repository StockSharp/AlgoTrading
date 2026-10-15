# Estrategia JBrainTrend ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una implementación en C# inspirada en el ejemplo de MQL5 "JBrainTrend1Stop_ReOpen".  
Usa el oscilador Estocástico para determinar condiciones de sobrecompra y sobreventa y soporta piramidación reabriendo posiciones cuando el precio avanza un paso específico.

## Lógica
- Suscribirse a velas del marco temporal seleccionado.
- Calcular el oscilador Estocástico (%K y %D).
- Entrar largo cuando %K cae por debajo de 20 y corto cuando %K sube por encima de 80.
- Las posiciones se cierran cuando se alcanza el extremo opuesto.
- Después de una entrada, se agregan posiciones adicionales si el precio se mueve `PriceStep` en la dirección de la operación, hasta `MaxPositions`.
- Stop-loss y take-profit de protección se aplican en unidades absolutas de precio.

## Parámetros
- `StochPeriod` – período principal del oscilador Estocástico.
- `KPeriod` / `DPeriod` – períodos de suavizado para las líneas %K y %D.
- `CandleType` – marco temporal usado para el análisis.
- `StopLoss` – distancia del stop-loss en unidades de precio.
- `TakeProfit` – distancia del take-profit en unidades de precio.
- `PriceStep` – movimiento de precio requerido para reabrir una posición.
- `MaxPositions` – número máximo de entradas en una dirección.
- `BuyEnabled` / `SellEnabled` – habilitar o deshabilitar operaciones largas/cortas.

## Notas
El script MQL5 original usaba un indicador personalizado llamado *JBrainTrend1Stop*.  
Este port en C# aproxima el concepto de trading con indicadores integrados de StockSharp para facilitar la integración.
