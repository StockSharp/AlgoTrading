# Estratégia de Cruzamento RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Cruzamento RVI utiliza o Índice de Vigor Relativo e um filtro de média móvel.
Compra quando o RVI cruza acima de sua linha de sinal enquanto o preço está abaixo da EMA, e vende quando o RVI cruza abaixo do sinal enquanto o preço está acima da EMA.

## Detalhes

- **Critérios de entrada**: RVI cruzando sua linha de sinal com filtro EMA vs VWMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `RviLength` = 10
  - `SignalLength` = 10
  - `EmaLength` = 31
  - `VwmaLength` = 1
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RVI, SMA, EMA, VWMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
