# Estratégia Color Zerolag RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Índice de Vigor Relativo e sua linha de sinal.
Compra quando a linha principal do RVI cruza abaixo da linha de sinal e vende quando a linha principal cruza acima da linha de sinal.

## Detalhes

- **Critérios de entrada**: Cruzamento do RVI e da linha de sinal
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4 horas
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RVI, SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (H4)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
