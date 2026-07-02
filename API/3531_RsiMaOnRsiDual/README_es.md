# RSI MA en RSI estrategia dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia dual RSI en RSI recrea el asesor experto MetaTrader "RSI_MAonRSI_Dual" dentro de StockSharp. Observa dos índices de fuerza relativa con diferentes períodos retrospectivos y aplica un promedio móvil común encima de cada secuencia RSI. Las decisiones comerciales se toman cuando las líneas RSI suavizadas se cruzan mientras permanecen en el mismo lado de un nivel neutral configurable.

La conversión mantiene el comportamiento del robot original, incluido el filtrado de tiempo y la capacidad de restringir la dirección comercial o invertir la lógica de la señal.

## Indicadores

- **Rápido RSI** – Índice de fuerza relativa con período configurable.
- **Lento RSI** – Índice de fuerza relativa con su propio período.
- **Promedio móvil en RSI**: promedio móvil simple calculado sobre cada flujo de valor de RSI. Ambos RSI utilizan la misma longitud de suavizado.

Los tres indicadores comparten el mismo precio aplicado (precio de cierre por defecto). Las dos líneas RSI suavizadas se dibujan en un panel de gráfico dedicado para el seguimiento.

## Reglas de entrada

1. Espere a que se formen ambos valores RSI suavizados en la barra completada actualmente.
2. **Configuración larga**
   - El RSI de suavizado rápido cruza **arriba** el RSI de suavizado lento (valor actual arriba, valor anterior abajo).
   - Ambos RSI suavizados están **por debajo** del nivel neutral (50 por defecto).
3. **Configuración corta**
   - El RSI de suavizado rápido cruza **abajo** el RSI de suavizado lento (valor actual debajo, valor anterior arriba).
   - Ambos RSI suavizados están **por encima** del nivel neutral.
4. Opcionalmente, invierta las direcciones de la señal utilizando el parámetro `ReverseSignals`.
5. Las señales generadas en la misma barra se ignoran (una entrada por barra).

## Gestión de posiciones

- `AllowLong` y `AllowShort` controlan si la estrategia puede abrir posiciones en cada dirección.
- `CloseOpposite` cierra una posición existente antes de ingresar al lado opuesto, replicando la lógica EA original.
- `OnlyOnePosition` prohíbe abrir una nueva posición cuando alguna posición ya está activa.
- Las órdenes de mercado se emiten con la estrategia `Volume`.

## Filtro de tiempo

Habilite o deshabilite el filtro de sesión comercial con `UseTimeFilter`. Cuando está habilitado, los intercambios solo se permiten entre `SessionStart` y `SessionEnd`. Se admiten sesiones que cruzan la medianoche. Las marcas de tiempo se evalúan en la zona horaria de intercambio proporcionada por los mensajes de vela entrantes.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas analizadas por la estrategia. |
| `FastRsiPeriod` | Período del ayuno RSI. |
| `SlowRsiPeriod` | Periodo de la lentitud RSI. |
| `MaPeriod` | Longitud de media móvil utilizada para suavizar ambas secuencias RSI. |
| `AppliedPrice` | Tipo de precio reenviado a los cálculos de RSI. |
| `NeutralLevel` | RSI umbral que separa zonas alcistas y bajistas. |
| `AllowLong` / `AllowShort` | Habilite o deshabilite la dirección comercial. |
| `ReverseSignals` | Intercambia señales largas y cortas. |
| `CloseOpposite` | Cierre la posición opuesta antes de ingresar a una nueva. |
| `OnlyOnePosition` | Permitir como máximo una posición abierta. |
| `UseTimeFilter` | Active el filtro de sesión de negociación. |
| `SessionStart` / `SessionEnd` | Límites de la ventana de negociación. |

## Diferencias con el EA original

- Los bloques de gestión de dinero, stop-loss y trailing-stop del código original MQL5 no se reproducen. La estrategia StockSharp coloca órdenes de mercado utilizando el `Volume` fijo configurado en la estrategia.
- Se eliminaron todas las alertas de registro y diagnóstico; En su lugar, se debe utilizar el registro StockSharp si es necesario.
- El seguimiento de transacciones específico de la plataforma se reemplaza con StockSharp eventos de estado de pedido.

A pesar de estas diferencias, la lógica de entrada central y los filtros direccionales coinciden con el asesor experto fuente.
