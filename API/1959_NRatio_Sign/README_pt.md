# Estratégia NRatio Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emprega o indicador NRatio, um oscilador baseado em NRTR que mede a distância normalizada entre o preço e um nível de rastreamento dinâmico. Os sinais de trading ocorrem quando o NRatio cruza limiares predefinidos. Dependendo do modo selecionado, o sistema reage a rompimentos além dos limites superior e inferior ou a reversões de volta para dentro deles.

A abordagem pode operar em ambos os lados do mercado e usa gestão de risco baseada em percentual para saídas. O suavizamento da métrica de distância é realizado com uma média móvel exponencial, permitindo que a estratégia responda rapidamente enquanto filtra o ruído.

## Detalhes

- **Critérios de entrada**:
  - **Modo In**:
    - **Comprado**: `NRatio` cruza acima de `UpLevel`.
    - **Vendido**: `NRatio` cruza abaixo de `DownLevel`.
  - **Modo Out**:
    - **Comprado**: `NRatio` cruza acima de `DownLevel`.
    - **Vendido**: `NRatio` cruza abaixo de `UpLevel`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop de proteção.
- **Stops**: Sim, take-profit e stop-loss em percentual.
- **Valores padrão**:
  - `CandleType` = velas de 4 horas
  - `Kf` = 1
  - `Length` = 3
  - `Fast` = 2
  - `Sharp` = 2
  - `UpLevel` = 80
  - `DownLevel` = 20
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: NRTR, EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
