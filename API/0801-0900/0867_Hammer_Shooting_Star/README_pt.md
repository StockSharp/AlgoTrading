# Estratégia Martelo e Estrela Cadente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera os padrões de velas Hammer e Shooting Star.
Uma posição comprada é aberta quando a vela anterior é um Hammer,
enquanto uma posição vendida segue uma Shooting Star.
As saídas usam a máxima e mínima da vela de sinal como take profit e stop loss.

## Detalhes

- **Critérios de entrada**:
  - Comprado: a vela anterior é um Hammer
  - Vendido: a vela anterior é uma Shooting Star
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss e take profit na mínima/máxima da vela de sinal
- **Stops**: Sim, fixos nos extremos da vela de sinal
- **Valores padrão**:
  - `WickFactor` = 0.9
  - `MaxOppositeWickFactor` = 0.45
  - `MinBodyRangePct` = 0.2
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Velas
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
