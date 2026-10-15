# Estratégia Genie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Genie é um consultor especialista baseado em Parabolic SAR aprimorado com o Índice Direcional Médio (ADX) para confirmar a força da tendência. A estratégia abre posições quando o SAR vira em relação ao preço enquanto os componentes +DI e -DI do ADX trocam de dominância. Um stop trailing e um take profit fixo gerenciam o risco.

Os testes mostram que a abordagem funciona melhor em instrumentos em tendência com volatilidade moderada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SAR anterior acima do fechamento anterior, SAR atual abaixo do fechamento atual, +DI anterior < -DI anterior, +DI atual > -DI atual, e ADX acima do +DI e -DI atuais.
  - **Vendido**: SAR anterior abaixo do fechamento anterior, SAR atual acima do fechamento atual, +DI anterior > -DI anterior, +DI atual < -DI atual, e ADX acima do +DI e -DI atuais.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O stop trailing é acionado ou o candle anterior fecha contra a posição.
- **Stops**: Sim, stop trailing e take profit medidos em unidades de preço.
- **Valores padrão**:
  - `TakeProfit` = 500
  - `TrailingStop` = 200
  - `SarStep` = 0.02
  - `AdxPeriod` = 14
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (entre +DI e -DI)
  - Nível de risco: Médio
