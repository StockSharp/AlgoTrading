# Estrategia Oscilador de Peso Directo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el experto de MetaTrader **Exp_WeightOscillator_Direct** dentro del API de alto nivel de StockSharp. Combina cuatro osciladores clásicos—RSI, Money Flow Index, Williams %R y DeMarker—en un único compuesto ponderado. La señal compuesta se suaviza mediante una media móvil configurable y se usa para detectar oscilaciones de momentum. Un compuesto en alza abre operaciones largas (o cierra cortos) cuando la estrategia trabaja en modo "Direct", mientras que el modo "Against" invierte la lógica para trading contrario.

## Cadena de indicadores
1. **Relative Strength Index (RSI)** – escala normalizada 0..100.
2. **Money Flow Index (MFI)** – oscilador sensible a la liquidez en rango 0..100.
3. **Williams %R (WPR)** – desplazado por +100 para alinearse con la escala 0..100.
4. **DeMarker** – multiplicado por 100 para coincidir con los otros osciladores.
5. **Media de suavizado** – una de las medias móviles soportadas (Simple, Exponencial, Suavizado, Ponderado, Jurik, Kaufman).
6. **Oscilador compuesto** – promedio ponderado de las entradas normalizadas, suavizado para eliminar ruido.

El valor del oscilador ponderado se almacena para cada vela terminada. Las señales analizan los últimos tres valores almacenados, opcionalmente omitiendo un número de barras más recientes mediante el parámetro *Signal Bar* para imitar el comportamiento del experto original.

## Lógica de trading
1. Esperar hasta que todos los indicadores y la media móvil de suavizado estén completamente formados.
2. Calcular el oscilador compuesto suavizado para la barra terminada actual y añadirlo al historial.
3. Recuperar tres valores históricos: `current`, `previous`, `prior`, con índices controlados por *Signal Bar*.
4. Detectar cambios de pendiente:
   - **Ascendente** cuando `previous < prior` **y** `current > previous`.
   - **Descendente** cuando `previous > prior` **y** `current < previous`.
5. Según el *Trend Mode* seleccionado:
   - **Direct**: operar con la pendiente (`ascendente` → señal larga, `descendente` → señal corta).
   - **Against**: operar contra la pendiente (`ascendente` → corto, `descendente` → largo).
6. Aplicar los interruptores de entrada/salida:
   - Cerrar la exposición opuesta si el interruptor *Close* correspondiente está habilitado.
   - Abrir nuevas posiciones solo si el interruptor *Allow* respectivo está habilitado. El tamaño de la orden equivale a `Volume + |Position|` para que la estrategia pueda girar de corto a largo (o viceversa) con una sola orden de mercado.
7. Las protecciones opcionales de stop-loss y take-profit se activan a través de `StartProtection` usando distancias expresadas en pasos de precio.

## Parámetros
| Grupo | Nombre | Descripción |
|-------|------|-------------|
| General | **Candle Type** | Marco temporal para suscripción de datos y cálculos de indicadores. |
| Trading | **Trend Mode** | `Direct` sigue la pendiente del oscilador, `Against` opera contratendencia. |
| Trading | **Signal Bar** | Número de barras cerradas más recientes a omitir (1 = última barra cerrada). |
| Oscillator | **RSI / MFI / WPR / DeMarker Weight** | Contribución relativa de cada oscilador en la mezcla ponderada. Cero deshabilita un componente. |
| Oscillator | **RSI / MFI / WPR / DeMarker Period** | Longitud de lookback para cada oscilador. |
| Oscillator | **Smoothing Method** | Media móvil aplicada al compuesto (Simple, Exponencial, Suavizado, Ponderado, Jurik, Kaufman). |
| Oscillator | **Smoothing Length** | Período para la media de suavizado. |
| Risk Management | **Stop Loss Points** | Distancia en pasos de precio; `0` deshabilita el stop. |
| Risk Management | **Take Profit Points** | Distancia en pasos de precio; `0` deshabilita el objetivo. |
| Trading | **Allow Long/Short Entries** | Habilitar o deshabilitar la apertura de nuevas posiciones largas/cortas. |
| Trading | **Close Shorts/Longs on Signal** | Permitir cerrar exposición existente cuando llega una señal opuesta. |

Todos los parámetros numéricos están expuestos como objetos `StrategyParam`, permitiendo optimización dentro del Designer de StockSharp.

## Notas de uso
- Configure la propiedad `Volume` base antes de iniciar la estrategia. Las órdenes de mercado se escalarán automáticamente al revertir posiciones.
- La estrategia se suscribe exactamente a una serie de velas devuelta por `GetWorkingSecurities()`.
- Los stops protectores usan el `PriceStep` del instrumento para convertir distancias en puntos a valores de precio absolutos.
- Cuando *Trend Mode* está configurado en `Against`, solo cambia la polaridad de la señal; todas las demás mecánicas permanecen idénticas al asesor experto original.
- Williams %R y DeMarker se normalizan para compartir la misma escala 0..100 que RSI/MFI, coincidiendo con la lógica del indicador original.

## Diferencias con el experto MQL
- El indicador original admitía tipos de suavizado adicionales (`ParMA`, `JurX`, `VIDYA`, `T3`). En StockSharp la estrategia ofrece contrapartes de alta calidad (Jurik y Kaufman) mientras usa Jurik por defecto para compatibilidad.
- Money Flow Index siempre usa el volumen agregado de la vela. MetaTrader podía alternar entre volúmenes de tick y reales; esta elección depende de la fuente de datos en StockSharp.
- La gestión de riesgos se implementa a través de `StartProtection` (basado en pasos de precio) en lugar de solicitudes basadas en puntos, pero ofrece el mismo comportamiento cuando `PriceStep` coincide con el tamaño del contrato del instrumento.

## Cómo empezar
1. Adjunte la estrategia a una cartera y un valor que admita el tipo de vela configurado.
2. Ajuste los pesos/períodos del indicador y habilite o deshabilite los interruptores de entrada.
3. Elija el método y longitud de suavizado que mejor se adapten a la volatilidad del instrumento.
4. Configure las distancias de stop-loss/take-profit en pasos de precio si se requiere protección.
5. Ejecute la estrategia; las señales solo se ejecutarán en velas terminadas, asegurando un comportamiento determinista.
