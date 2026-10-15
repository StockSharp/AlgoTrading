# Estratégia RSI Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o expert advisor original **iRSISign** do MQL5 para a API de alto nível do StockSharp. Combina o Índice de Força Relativa (RSI) com o Average True Range (ATR) para gerar sinais de entrada e saída.

O sistema escuta velas finalizadas de um período definido pelo usuário. Quando o RSI cruza acima do limiar inferior, sinaliza uma possível reversão de alta e abre uma posição comprada ou fecha uma posição vendida existente. Por outro lado, quando o RSI cai abaixo do limiar superior, entra em uma posição vendida ou fecha uma posição comprada ativa. O ATR é calculado mas usado apenas como contexto adicional, espelhando o indicador original que exibia setas de sinal deslocadas por ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O valor anterior do RSI estava abaixo de `DownLevel` e o RSI atual cruza acima.
  - **Vendido**: O valor anterior do RSI estava acima de `UpLevel` e o RSI atual cruza abaixo.
- **Comprado/Vendido**: Ambas as direções são permitidas e podem ser habilitadas independentemente.
- **Critérios de saída**:
  - O sinal oposto fecha a posição atual se o sinalizador de fechamento correspondente estiver ativo.
- **Stops**: Não implementados. Gestão de risco pode ser adicionada externamente se necessário.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `UpLevel` = 70
  - `DownLevel` = 30
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Flexível
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `RsiPeriod` | Comprimento do RSI. |
| `AtrPeriod` | Comprimento do ATR. |
| `UpLevel` | Limiar superior do RSI que gera sinais de venda. |
| `DownLevel` | Limiar inferior do RSI que gera sinais de compra. |
| `CandleType` | Período das velas usado para cálculos. |
| `BuyOpen` | Habilitar abertura de posições compradas. |
| `SellOpen` | Habilitar abertura de posições vendidas. |
| `BuyClose` | Permitir fechamento de posições compradas com sinal oposto. |
| `SellClose` | Permitir fechamento de posições vendidas com sinal oposto. |

A estratégia é destinada como exemplo educacional demonstrando como traduzir a lógica simples do MQL5 para o framework de estratégias de alto nível do StockSharp.
