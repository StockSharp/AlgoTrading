# Estrategia Fractal MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción del asesor experto `Exp_Fractal_MFI.mq5`. Utiliza el indicador Money Flow Index (MFI) para generar señales de trading cuando el oscilador cruza niveles superiores e inferiores predefinidos.

## Cómo funciona
- Calcula el MFI durante un período configurable.
- Cuando el valor anterior del MFI estaba por encima del **Nivel Bajo** y el valor actual cae por debajo, se genera una señal.
  - En modo **Direct**, esto abre una posición larga y opcionalmente cierra cortos.
  - En modo **Against**, esto abre una posición corta y opcionalmente cierra largos.
- Cuando el valor anterior del MFI estaba por debajo del **Nivel Alto** y el valor actual sube por encima, se genera otra señal.
  - En modo **Direct**, esto abre una posición corta y opcionalmente cierra largos.
  - En modo **Against**, esto abre una posición larga y opcionalmente cierra cortos.

Solo se procesan velas completadas. La estrategia puede configurarse para habilitar o deshabilitar la apertura y cierre de posiciones largas o cortas por separado.

## Parámetros
- `MfiPeriod` – período del cálculo del Money Flow Index.
- `HighLevel` – umbral superior para el MFI.
- `LowLevel` – umbral inferior para el MFI.
- `CandleType` – marco temporal de velas usado en los cálculos.
- `Trend` – elegir `Direct` para operar en la dirección del indicador o `Against` para invertir las señales.
- `BuyPosOpen` / `SellPosOpen` – permitir apertura de posiciones largas o cortas.
- `BuyPosClose` / `SellPosClose` – permitir cierre de posiciones existentes en señales opuestas.

## Notas
Esta versión en C# se enfoca en el uso de la API de alto nivel y no implementa las reglas de gestión de dinero originales ni los niveles de stop del código MQL.
