# Estratégia ADX MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
ADX MACD combina a força de tendência do Average Directional Index com as mudanças de momentum do MACD.
Quando o ADX está em alta, os rompimentos têm maior probabilidade de continuar, especialmente se o MACD cruzar na mesma direção.

Os testes indicam um retorno anual médio de aproximadamente 139%. Funciona melhor no mercado de ações.

A estratégia opera esses sinais alinhados e sai quando o ADX começa a enfraquecer ou o MACD vira contra a posição.

Um stop percentual moderado contém as perdas durante mercados instáveis.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

