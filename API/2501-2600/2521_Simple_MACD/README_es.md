# Estrategia Simple MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Simple MACD replica la lógica del asesor MQL5 `Simple_MACD.mq5` en StockSharp. La estrategia sigue la pendiente de la línea principal del MACD calculada en velas completadas y continúa añadiendo a la posición siempre que la pendiente permanezca en la misma dirección.

## Descripción general

- **Mercado**: cualquier instrumento con datos de velas y horario de trading continuo.
- **Indicador principal**: Convergencia/Divergencia de Medias Móviles (MACD) usando medias móviles exponenciales 12/26 y señal 9.
- **Enfoque**: seguimiento de momentum. La estrategia compara las dos lecturas de MACD completadas más recientes y va largo cuando la línea sube, o corto cuando la línea cae.
- **Tipo de orden**: solo órdenes de mercado. Cada señal agrega la cantidad requerida para cerrar la posición opuesta y añade el volumen de trading configurado encima, imitando el asesor experto original.

## Notas de Conversión

- El bot MQL5 se activaba una vez por nueva barra comparando `MACD(1)` y `MACD(2)` (las dos barras completadas anteriores). En StockSharp, la misma comparación se ejecuta cuando una vela termina, antes de que comience la siguiente barra.
- La versión MQL dependía de la enumeración explícita de posiciones y verificaciones manuales de volumen. La versión StockSharp agrega el volumen automáticamente con llamadas `BuyMarket`/`SellMarket` y el parámetro `TradeVolume` de la estrategia.
- Las verificaciones de cobertura del código MQL no son necesarias porque StockSharp rastrea directamente la posición neta.

## Reglas de Trading

### Entrada y Escalado

1. Calcular la línea principal del MACD en cada vela terminada.
2. Almacenar los dos últimos valores del MACD y compararlos:
   - Si `MACD(1) > MACD(2)`, la pendiente es alcista. La estrategia compra un volumen igual a `TradeVolume + max(0, -Posición)` para cerrar cortos y añadir nuevos largos.
   - Si `MACD(1) < MACD(2)`, la pendiente es bajista. La estrategia vende `TradeVolume + max(0, Posición)` para cerrar largos y añadir nuevos cortos.
3. Si ambos valores son iguales, no se envían nuevas órdenes.

### Gestión de Posiciones

- La estrategia continúa apilando órdenes en la dirección actual mientras la pendiente del MACD no cambie de signo, al igual que el asesor original que enviaba una compra o venta en cada barra calificada.
- Las señales opuestas eliminan cualquier exposición abierta antes de construir la nueva posición.
- No se incorporan niveles de stop-loss ni toma de ganancias; el control de riesgo depende de reglas externas de gestión de dinero o supervisión manual.

### Salvaguardas Adicionales

- El trading se omite hasta que el indicador MACD esté completamente formado.
- Solo se procesan velas completadas (`CandleStates.Finished`), previniendo acciones prematuras en datos parciales.
- Los mensajes de registro rastrean cada operación y muestran los dos valores del MACD usados para tomar la decisión para un análisis de backtesting más fácil.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|-----------|----------------------|-------------|
| `FastPeriod` | 12 | Longitud de la EMA rápida para el cálculo del MACD. |
| `SlowPeriod` | 26 | Longitud de la EMA lenta para el cálculo del MACD. |
| `SignalPeriod` | 9 | Período de la EMA de señal retenido para compatibilidad con la configuración original. |
| `TradeVolume` | 0.1 | Volumen añadido en cada señal antes de contabilizar la reversión de posición. |
| `CandleType` | Marco temporal de 1 minuto | Tipo de vela usado para alimentar el indicador. Ajustable a cualquier marco temporal deseado. |

Todos los parámetros están expuestos como parámetros de estrategia y marcados como optimizables donde sea relevante.

## Visualización

- La estrategia crea automáticamente un área de gráfico (cuando está disponible) con las velas de precio y superpone la salida del indicador MACD.
- Las operaciones propias se dibujan en el gráfico para mostrar con qué frecuencia la estrategia escala posiciones en condiciones de tendencia.

## Uso Recomendado

- Aplicar en instrumentos con tendencia donde el momentum persiste por varias barras; los mercados en rango causarán frecuentes reversiones y operaciones whipsaw.
- Combinar con gestión de riesgo a nivel de portafolio ya que la lógica base no tiene mecanismo de stop intrínseco.
- Considerar optimizar el `TradeVolume` y los períodos del MACD para el instrumento y marco temporal objetivo.

## Archivos

- `CS/SimpleMacdStrategy.cs` – implementación en StockSharp de la lógica de la estrategia.
- `README.md`, `README_ru.md`, `README_zh.md` – documentación detallada en tres idiomas.
