# Estrategia de Vhf Sliding Windows
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertida del asesor experto de MetaTrader 5 **"VHF EA"** por Vladimir Karputov.
- Usa el indicador Vertical Horizontal Filter (VHF) para clasificar el régimen de mercado como tendencial o en rango.
- Funciona en cualquier instrumento y marco temporal compatible con StockSharp; simplemente cambia el parámetro de tipo de vela para que coincida con el gráfico deseado.

## Lógica de negociación
1. Suscribirse a la serie de velas seleccionada y calcular el indicador VHF con período `VhfPeriod` en cada vela terminada.
2. Mantener dos ventanas deslizantes de valores VHF recientes:
   - **Ventana principal (`MainWindowSize`)** – establece el rango VHF general y el punto medio.
   - **Ventana de trabajo (`WorkingWindowSize`)** – detecta rupturas a corto plazo por encima o por debajo de la mediana VHF local.
3. Un régimen de tendencia alcista o bajista se confirma solo cuando el valor VHF actual es mayor que el punto medio de ambas ventanas.
4. Mientras está en régimen de tendencia, comparar el último precio de cierre con el cierre hace `MainWindowSize` barras:
   - Cierre más alto que la referencia → el comportamiento predeterminado es abrir/mantener una posición larga.
   - Cierre más bajo que la referencia → el comportamiento predeterminado es abrir/mantener una posición corta.
   - Habilitar `ReverseSignals` para invertir estas direcciones.
5. La estrategia cierra cualquier posición abierta cuando el valor VHF cae de nuevo dentro de la zona de rango (el VHF actual no está por encima de ambos puntos medios).
6. Los cambios de posición se manejan comprando/vendiendo suficiente volumen para cerrar el lado opuesto y abrir el nuevo en una sola orden de mercado.

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|---------|-------|
| `MainWindowSize` | Número de valores VHF en la ventana deslizante principal. | `11` | Debe ser mayor que `WorkingWindowSize`. |
| `WorkingWindowSize` | Número de valores VHF en la ventana secundaria. | `7` | Proporciona confirmación más rápida de rupturas. |
| `VhfPeriod` | Período de retrovisión del Vertical Horizontal Filter. | `9` | Determina la sensibilidad del indicador. |
| `Volume` | Volumen de orden (lotes) utilizado para nuevas entradas. | `1` | Se agrega al valor de posición actual absoluto al cambiar dirección. |
| `ReverseSignals` | Invertir la lógica largo/corto derivada de la dirección del precio. | `true` | Coincide con el comportamiento predeterminado del EA original. |
| `CandleType` | Marco temporal y tipo de vela para la suscripción de datos. | `Marco temporal de 15 minutos` | Cambiar para adaptar la estrategia a otros gráficos. |

## Gestión monetaria y salidas
- La estrategia siempre opera con un volumen fijo definido por `Volume`.
- La gestión de stop protector se delega al asistente integrado `StartProtection()` de StockSharp, que cierra de forma segura posiciones residuales inesperadas.
- No se codifican objetivos de stop-loss o take-profit; las salidas dependen del cambio de régimen detectado por VHF.

## Notas de implementación
- Usa la API de suscripción de velas de alto nivel con vinculación de indicadores, siguiendo las directrices del proyecto.
- Un indicador personalizado Vertical Horizontal Filter idéntico a la versión MQL está integrado en la estrategia.
- Las declaraciones de registro describen cada cambio de posición y transición de régimen para facilitar la depuración.
