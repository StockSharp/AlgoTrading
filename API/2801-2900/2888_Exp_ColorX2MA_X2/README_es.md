# Estrategia Exp ColorX2MA X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia recrea el experto de doble marco temporal "Exp_ColorX2MA_X2" para StockSharp. Utiliza dos filtros ColorX2MA: un mapa de tendencia en el marco temporal superior y un disparador de entrada en el marco temporal inferior. Ambos valores ColorX2MA se construyen encadenando dos medias móviles configurables y luego coloreando el resultado según la pendiente actual. Las decisiones de trading se ejecutan cuando el color del marco temporal inferior cambia en la dirección de la tendencia del marco temporal superior.

La implementación soporta las opciones de precio aplicado originales y los modos de suavizado más comunes (SMA, EMA, SMMA, LWMA, Jurik). Cuando el indicador Jurik expone una propiedad `Phase`, se actualiza con el valor de fase configurado.

## Reglas de Trading
- **Entrada en largo**
  - El color ColorX2MA del marco temporal superior es alcista (trend direction > 0).
  - El color ColorX2MA del marco temporal inferior cambió de alcista en la barra anterior a neutro o bajista en la última barra completada (`Clr[1] == 1` y `Clr[0] != 1`).
  - El trading en largo está habilitado.
- **Entrada en corto**
  - El color ColorX2MA del marco temporal superior es bajista (trend direction < 0).
  - El color ColorX2MA del marco temporal inferior cambió de bajista en la barra anterior a neutro o alcista en la última barra completada (`Clr[1] == 2` y `Clr[0] != 2`).
  - El trading en corto está habilitado.
- **Salida en largo**
  - Cuando aparece un color bajista en el marco temporal inferior (`Clr[1] == 2`) y el permiso de cierre de largo secundario está habilitado, **o** la tendencia del marco temporal superior gira bajista con el permiso de cierre de largo primario habilitado.
- **Salida en corto**
  - Cuando aparece un color alcista en el marco temporal inferior (`Clr[1] == 1`) y el permiso de cierre de corto secundario está habilitado, **o** la tendencia del marco temporal superior gira alcista con el permiso de cierre de corto primario habilitado.
- **Stops**
  - Las distancias opcionales de stop loss y take profit se especifican en puntos (multiplicadas por el paso de precio del instrumento). Se evalúan en cada vela de señal finalizada comparando los extremos de la vela con el precio promedio de la posición.

## Valores predeterminados
- **Marco temporal de tendencia**: velas de 6 horas.
- **Marco temporal de señal**: velas de 30 minutos.
- **Suavizado de tendencia**: SMA(12) alimentando a Jurik(5, fase 15).
- **Suavizado de señal**: SMA(12) alimentando a Jurik(5, fase 15).
- **Precio aplicado**: Cierre.
- **Desplazamiento de señal**: 1 barra en ambos marcos temporales.
- **Permisos**: entradas y salidas en largo/corto habilitadas.
- **Stop loss**: 1000 puntos (convertido usando el paso de precio).
- **Take profit**: 2000 puntos (convertido usando el paso de precio).

## Filtros y Notas
- Dirección: opera en largo y corto, controlado mediante indicadores de permiso.
- Marco temporal: doble marco temporal (tendencia en HTF, entradas en LTF).
- Indicadores: ColorX2MA de dos niveles con métodos de suavizado configurables.
- Soporte de suavizado: `Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`. Otros modos de la librería original no están implementados.
- Precios aplicados: las 12 fórmulas originales incluyendo precios TrendFollow y Demark.
- Stops: stop loss y take profit opcionalmente a distancia fija.
- Complejidad: intermedio porque sincroniza dos marcos temporales y búferes de color.
- Adecuado para: configuraciones de seguimiento de tendencia en FX, índices o cripto donde se prefiere el indicador ColorX2MA.

## Consejos de Uso
- Mantener el marco temporal superior significativamente mayor que el marco temporal de señal para evitar whipsaws frecuentes.
- Ajustar el parámetro de desplazamiento de señal (`SignalSignalBar`) para reaccionar más rápido o aumentarlo para suavizar más.
- Si el instrumento no provee `PriceStep`, las distancias de stop/take se interpretan directamente en unidades de precio.
- El suavizado Jurik requiere un paquete de indicadores StockSharp con licencia; cuando no esté disponible, la estrategia funciona con las otras opciones de suavizado.
