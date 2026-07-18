# Estrategia clásica MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia MA2CCI incorpora el clásico asesor experto MetaTrader creado en torno a la interacción de dos promedios móviles simples (SMA) y el índice del canal de productos básicos (CCI). Filtra operaciones utilizando la línea cero CCI y aplica paradas protectoras derivadas del rango verdadero promedio (ATR). El sistema está diseñado para entradas que siguen tendencias con una rápida reacción a las reversiones.

La versión StockSharp mantiene la lógica comercial original al tiempo que adapta la gestión de riesgos al entorno .NET. El tamaño de la posición sigue una regla de riesgo por mil con un factor de disminución adicional que reduce el tamaño de la operación después de pérdidas consecutivas. Cada entrada adjunta una parada impulsada por la volatilidad que refleja la distancia ATR utilizada en la implementación MQL.

## Lógica de trading

- **Indicadores**
  - Rápido SMA con longitud predeterminada 4.
  - Lento SMA con longitud predeterminada 8.
  - Filtro CCI usando retrospectiva de 4 períodos.
  - ATR con periodo 4 para colocación de parada.
- **Condiciones de entrada**
  - **Largo**: el SMA rápido cruza por encima del SMA lento y la barra terminada anterior muestra CCI subiendo hasta cero (de negativo a positivo).
  - **Corto**: el SMA rápido cruza por debajo del SMA lento y la barra anterior muestra el CCI cayendo por cero (de positivo a negativo).
- **Condiciones de salida**
  - El cruce opuesto SMA cierra posiciones abiertas incluso si no se inicia ninguna nueva operación.
  - parada ATR: las posiciones largas salen cuando el precio cae a `entry - ATR`; las posiciones cortas salen cuando el precio sube a `entry + ATR`.

## Gestión del riesgo

- El volumen de pedido base es configurable; por defecto 0,1 lotes (o equivalente en intercambio).
- El tamaño dinámico opcional escala el volumen a `free capital * MaxRiskPerThousand / 1000` cuando los datos de la cartera están disponibles.
- Después de más de una pérdida consecutiva, el tamaño de la posición se reduce linealmente en `losses / DecreaseFactor` del volumen calculado.
- Las paradas de volatilidad dependen de la vela terminada más reciente; Los picos intrabar más allá de los niveles de parada desencadenan una salida del mercado en el siguiente tick de la estrategia.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo de trabajo para todos los indicadores. | velas de 1 hora |
| `OrderVolume` | Tamaño mínimo de operación cuando el tamaño basado en el riesgo no está disponible. | 0.1 |
| `FastMaPeriod` | Período del ayuno SMA. | 4 |
| `SlowMaPeriod` | Periodo de la lentitud SMA. | 8 |
| `CciPeriod` | Periodo del filtro CCI. | 4 |
| `AtrPeriod` | ATR longitud para el cálculo de parada. | 4 |
| `MaxRiskPerThousand` | Fracción de capital libre asignada por operación (por 1000 unidades). | 0,02 |
| `DecreaseFactor` | El divisor solía reducir el volumen después de rachas perdedoras. | 3 |

## Notas

1. La estrategia solo procesa velas terminadas, lo que garantiza una decisión por barra similar a la EA original que usaba `Volume[0] > 1` como puerta.
2. Los niveles de parada se simulan internamente en lugar de registrar órdenes de parada de cambio; esto coincide con el comportamiento de la versión MetaTrader que dependía de los cierres del mercado cuando se alcanzaban los umbrales ATR.
3. Habilite los gráficos dentro de StockSharp Designer para visualizar SMA, CCI y las operaciones ejecutadas utilizando los asistentes de dibujo integrados.
