# Estrategia Pivot Heiken
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina puntos pivote diarios con velas Heikin-Ashi y un trailing stop opcional. El pivote diario se calcula a partir del máximo, mínimo y cierre del día anterior. El suavizado de Heikin-Ashi filtra el ruido del precio y destaca la dirección de la tendencia.

## Lógica
- **Entrada larga**: La vela Heikin-Ashi es alcista y el cierre está por encima del pivote diario.
- **Entrada corta**: La vela Heikin-Ashi es bajista y el cierre está por debajo del pivote diario.
- **Salida**: La posición sale en el nivel de stop loss, take profit o trailing stop.

## Parámetros
- `CandleType` – serie de velas de trabajo.
- `StopLossPips` – distancia del stop loss en pips.
- `TakeProfitPips` – distancia del take profit en pips.
- `TrailingStopPips` – distancia del trailing stop en pips (0 desactiva el trailing).

## Indicadores
- Heikin-Ashi (calculado internamente).
- Punto pivote diario.

## Notas
- Usa la API de alto nivel con suscripciones de velas y enlace de indicadores.
- Adecuada para operaciones tanto largas como cortas.
