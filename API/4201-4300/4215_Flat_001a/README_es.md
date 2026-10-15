# Estrategia de rango plano 001a
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Flat 001a es un sistema de especulación diseñado para el gráfico horario del EURUSD. Escanea las velas de tres horas más recientes y mide la distancia entre el máximo más alto y el mínimo más bajo. Cuando el rango de esta ventana de tres velas se mantiene dentro de un número configurable de puntos, la estrategia anticipa que el precio permanecerá atrapado dentro del piso. Luego intenta limitar las incursiones breves en el cuarto superior o inferior del canal y emite inmediatamente órdenes de protección.

El asesor experto original MQL4 negoció solo EURUSD en el primer semestre y rechazó la negociación si el símbolo o el período de tiempo eran incorrectos. Este puerto mantiene los mismos valores predeterminados (EURUSD, velas de 60 minutos) y reproduce todos los cálculos de entrada, stop-loss, take-profit y trailing-stop en StockSharp.

## Indicadores y datos
- Los indicadores `Highest` y `Lowest` (período = 3) rastrean la parte superior e inferior de las últimas tres velas terminadas.
- Un parámetro de marco de tiempo tiene como valor predeterminado velas de 60 minutos para reflejar el requisito H1 del código fuente.
- No se utilizan osciladores adicionales ni filtros de suavizado, por lo que la estrategia reacciona únicamente a los precios extremos.

## Lógica de entrada
1. Espere a que se cierre la vela de suscripción. Sólo se procesan velas terminadas.
2. Verifique que el código de seguridad actual coincida con el código configurado (predeterminado: `EURUSD`). Si no es así, la estrategia queda inactiva.
3. Evalúe la ventana de negociación opcional. De forma predeterminada, se permiten entradas durante las dos horas que comienzan a la medianoche, hora de la plataforma (horas 0 y 1). El filtro se puede desactivar.
4. Calcule el rango de tres velas `range = highest - lowest` y tradúzcalo a puntos mediante el instrumento `PriceStep`.
5. Continúe solo si el número de puntos se encuentra entre `DiffMinPoints` y `DiffMaxPoints`.
6. Si el precio de cierre se encuentra dentro del cuarto más bajo del rango y no hay ninguna posición abierta, inicie una operación larga.
7. Si el precio de cierre se encuentra dentro del cuarto más alto del rango y no hay ninguna posición abierta, ingrese una operación corta.

## Gestión de pedidos
- **Stop-loss inicial**
  - Operaciones largas: `lowest - range / 3`.
  - Operaciones cortas: `highest + range / 3`.
- **Take-profit**
  - Operaciones largas: precio de entrada + `TakeProfitPoints * PriceStep`.
  - Operaciones cortas: precio de entrada - `TakeProfitPoints * PriceStep`.
- **Parada de seguimiento**
  - Una vez que el beneficio no realizado supera `TrailingStopPoints * PriceStep`, el stop-loss se sigue vela por vela.
  - Las operaciones largas mueven el stop a `closePrice - TrailingDistance` si es más alto que el stop actual.
  - Las operaciones cortas mueven el tope a `closePrice + TrailingDistance` si es inferior al tope actual.
- Todas las salidas se ejecutan con órdenes de mercado. La estrategia cierra la posición completa cuando la vela siguiente toca el nivel de stop-loss o take-profit.

## Parámetros
| grupo | Nombre | Descripción | Predeterminado |
| --- | --- | --- | --- |
| generales | `CandleType` | Tipo de vela utilizada para los cálculos. Debe establecerse en un período de tiempo de 60 minutos para que coincida con el sistema original. | `TimeFrame(60m)` |
| generales | `SecurityCode` | Código de seguridad esperado. Déjelo vacío para operar con cualquier instrumento. | `EURUSD` |
| Filtro de rango | `DiffMinPoints` | Rango mínimo de tres velas en puntos necesarios para operar. | `18` |
| Filtro de rango | `DiffMaxPoints` | Rango máximo de tres velas en puntos permitidos para operar. | `28` |
| Ventana de negociación | `EnableTimeFilter` | Activa o desactiva el filtro horario. | `true` |
| Ventana de negociación | `OpenHour` | Hora de inicio (0–23) de la ventana de negociación. La estrategia también permite la próxima hora inmediata. | `0` |
| Gestión de riesgos | `TakeProfitPoints` | Distancia de toma de ganancias expresada en puntos. Establezca en cero para desactivar. | `8` |
| Gestión de riesgos | `TrailingStopPoints` | Distancia del trailing stop expresada en puntos. Establezca en cero para desactivar el seguimiento. | `6` |

## Notas practicas
- La propiedad StockSharp `Strategy.Volume` controla el tamaño del pedido. Ajústelo según el tamaño del contrato de su corredor.
- Asegúrese de que el instrumento seleccionado exponga un `PriceStep` válido. Si falta `PriceStep`, la estrategia vuelve a `1` y registra una advertencia.
- El asesor experto MQL4 ofreció administración de dinero opcional escalando lotes según el saldo de la cuenta. La muestra de StockSharp mantiene constante el tamaño de la posición; puede programar su propia gestión de volumen si es necesario.
- Pruebe siempre la estrategia en simulación antes de ejecutarla en vivo. La lógica de seguimiento supone que el corredor ejecutará órdenes de protección cuando los extremos de las velas crucen el nivel; En los mercados rápidos, el deslizamiento puede aumentar el riesgo realizado.
