# Estrategia CDC PL RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia CDC PL RSI** replica el MQL Asesor Experto *Expert_ADC_PL_RSI* dentro del ecosistema StockSharp. El sistema escanea velas terminadas en busca de patrones de reversión de velas japonesas y confirma las entradas con el Índice de Fuerza Relativa (RSI). Las operaciones largas se basan en el patrón *Piercing Line* durante condiciones de sobreventa RSI, mientras que las operaciones cortas requieren el patrón *Dark Cloud Cover* combinado con lecturas de sobrecompra RSI. El enfoque mantiene simple el concepto original de administración del dinero al utilizar el volumen de la estrategia y permitir que StockSharp maneje el tamaño de la posición.

## Lógica de patrones e indicadores
- **Patrones de velas**: la estrategia reconstruye la lógica MetaTrader analizando las dos últimas velas terminadas. Las reglas de Línea perforante y Cobertura de nube oscura reflejan el código original, incluidas comprobaciones de espacios, cuerpos largos en relación con un promedio de cuerpo adaptativo y la dirección de la tendencia subyacente.
- Filtro **RSI**: un RSI de 20 períodos (optimizable) confirma el impulso. Las lecturas de sobreventa (`RSI < 40`) desbloquean entradas largas y las lecturas de sobrecompra (`RSI > 60`) desbloquean entradas cortas. El historial RSI también se utiliza para detectar salidas cuando el oscilador cruza los niveles 30 o 70 en la dirección opuesta.
- **Promedio del cuerpo y filtro de tendencia**: un promedio móvil simple de los tamaños del cuerpo de las velas y otro SMA de precios de cierre replican las MetaTrader funciones auxiliares (`AvgBody` y `CloseAvg`). Estos promedios previenen las señales durante el ruido y hacen que los patrones aparezcan después de un movimiento claro.

## Reglas de trading
### Configuración larga
1. Detecte un patrón de Línea Perforante en las dos últimas velas completadas.
2. Requerir que RSI de la vela terminada anterior esté por debajo de 40.
3. Si las condiciones se mantienen, compre en el mercado. Cuando existe una posición opuesta, la estrategia se revierte comprando el tamaño absoluto de la posición más el volumen configurado.

### Configuración corta
1. Detecte un patrón de cobertura de nubes oscuras en las dos últimas velas.
2. Requerir que RSI de la vela terminada anterior esté por encima de 60.
3. Si las condiciones se mantienen, venda en el mercado. Una posición opuesta se cierra y se invierte usando la misma lógica de volumen.

### Condiciones de salida
- Cierre posiciones largas cuando RSI cruce hacia abajo hasta 70 o cruce hacia arriba hasta 30, lo que indica que el impulso se ha desvanecido o revertido.
- Cierre las posiciones cortas cuando RSI cruce hacia arriba hasta 30 o cruce hacia abajo hasta 70, reflejando la implementación de MetaTrader.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `RsiPeriod` | 20 | RSI longitud retrospectiva. Optimizable entre 10 y 40 en pasos de 5. |
| `BodyAveragePeriod` | 14 | Período tanto para el tamaño promedio del cuerpo de la vela como para el filtro de tendencia del precio de cierre. Optimizable entre 10 y 30 en pasos de 5. |
| `CandleType` | plazo de 1 hora | Serie de velas utilizadas para los cálculos. Se puede seleccionar cualquier tipo de vela compatible con StockSharp. |
| `Volume` (clase base) | — | Volumen comercial establecido en la instancia de la estrategia antes del lanzamiento. |

## Uso
1. Adjunte la estrategia a una cartera y seguridad en StockSharp Designer, Shell o Runner.
2. Configure el tipo de vela y el volumen según el mercado en el que se opera.
3. Opcionalmente, ajuste los períodos RSI y promedio corporal para que coincidan con la volatilidad del instrumento o realice optimizaciones usando StockSharp Optimizer.
4. Inicie la estrategia y supervise las superposiciones del gráfico (velas, RSI y línea de promedio cercano) para revisar las confirmaciones de patrones y las operaciones ejecutadas.

## Notas
- La estrategia llama a `StartProtection()` para que se puedan configurar rutinas de protección integradas si es necesario (stop-loss, take-profit, trailing, etc.).
- Solo se procesan velas completadas, manteniendo la lógica coherente con el Asesor Experto MQL.
- No se almacenan colecciones adicionales; Las instancias de indicadores llevan los cálculos de ventana deslizante necesarios para las comprobaciones de patrones.
