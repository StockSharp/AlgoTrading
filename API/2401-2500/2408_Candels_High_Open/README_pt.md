# Estratégia Candels High Open
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que negocia quando uma vela abre exatamente em sua máxima ou mínima.
Uma posição comprada é aberta se a abertura da vela for igual à sua mínima, antecipando movimento de alta.
Uma posição vendida é aberta se a abertura da vela for igual à sua máxima, esperando uma queda.
A posição é fechada quando o preço cruza o valor do Parabolic SAR, atuando como saída trailing.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Open == Low`
  - Vendido: `Open == High`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O preço cruza o Parabolic SAR ou sinal oposto
- **Stops**: Usa níveis fixos de stop loss e take profit
- **Valores padrão**:
  - `StopLevel` = 50m
  - `TakeLevel` = 50m
  - `SarStep` = 0.02m
  - `SarMax` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `ReverseSignals` = false
- **Filtros**:
  - Categoria: Ação do preço
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
