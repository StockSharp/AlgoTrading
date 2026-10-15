# Cinco estrategias MA de múltiples plazos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Five MA Multi-Timeframe** replica el asesor experto original MT4 "5matf" utilizando el API de alto nivel de StockSharp. La estrategia analiza cinco promedios móviles simples en tres períodos de tiempo (primario, superior y más lento) y combina la pendiente de cada promedio con el Oscilador Acelerador para producir señales de entrada graduadas. Cuando hay suficiente evidencia alcista o bajista en todos los plazos, la estrategia abre o cierra posiciones en consecuencia.

## Indicadores y datos
- **Promedios móviles simples (SMA)**: Períodos 5, 8, 13, 21 y 34 en los tres períodos de tiempo.
- **Oscilador del acelerador (AC)**: Aplicado en los períodos de tiempo primario y terciario para evaluar la aceleración del impulso.
- **Períodos de tiempo**: predeterminado: 15 minutos (señal), 60 minutos (confirmación) y 240 minutos (filtro de tendencias). Todos los plazos se pueden ajustar mediante parámetros.

## Lógica de señal
1. Cada SMA compara su valor actual con la vela anterior para determinar una pendiente ascendente o descendente.
2. Accelerator Oscillator busca secuencias alcistas o bajistas utilizando los últimos cuatro valores.
3. Los recuentos de pendientes y las contribuciones del oscilador se agregan en puntuaciones porcentuales para cada período de tiempo.
4. Cuando los tres marcos temporales tienen puntuaciones alcistas superiores al 50%, se genera una señal de **COMPRA**. Las puntuaciones superiores al 75% refuerzan la señal.
5. Los mismos umbrales aplicados en la dirección opuesta generan señales de **VENTA**.
6. Las posiciones se cierran cuando una señal opuesta excede el nivel de cierre configurado. Las nuevas operaciones sólo se abren cuando no hay ninguna posición activa, lo que refleja el comportamiento original del asesor experto.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | velas de 15 minutos | Marco de tiempo principal utilizado para las señales comerciales. |
| `HigherTimeframe1` | velas de 60 minutos | Primer plazo más alto para la confirmación. |
| `HigherTimeframe2` | velas de 240 minutos | Segundo período de tiempo más alto para el filtro de tendencia lenta. |
| `FirstPeriod` – `FifthPeriod` | 5, 8, 13, 21, 34 | SMA longitudes aplicadas a cada período de tiempo. |
| `OpenLevel` | 0 | Calificación de señal mínima requerida para abrir una nueva posición. |
| `CloseLevel` | 1 | Se requiere grado de señal opuesto para cerrar una posición existente. |

Todos los parámetros se pueden optimizar o ajustar dentro de la interfaz de usuario de estrategia de StockSharp.

## Notas de uso
- La estrategia utiliza órdenes de mercado y no emite reversiones simultáneas; siempre espera una posición plana antes de abrirse en la dirección opuesta.
- Habilite las fuentes de datos históricos para todos los períodos de tiempo seleccionados para garantizar cálculos sincronizados.
- Considere ajustar las longitudes de SMA o el uso del oscilador al aplicar la estrategia a diferentes mercados o regímenes de volatilidad.

## Notas de conversión
Esta implementación conserva el comportamiento principal del asesor experto MT4 "5matf" al tiempo que aprovecha el sistema de vinculación de indicadores y suscripción de StockSharp. La lógica del acelerador requiere cuatro velas completas antes de que las señales se activen, al igual que el guión original.
