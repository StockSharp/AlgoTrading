# Three White Soldiers Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O padrão Três Soldados Brancos é uma reversão de alta clássica composta por três velas de alta fortes consecutivas. Após uma tendência de baixa, essa sequência frequentemente marca o início de um movimento ascendente sustentado, pois a pressão compradora supera os vendedores.

Os testes indicam um retorno anual médio de aproximadamente 175%. Funciona melhor no mercado de ações.

A estratégia entra comprada assim que o terceiro soldado se forma, esperando continuação do aumento de momentum. Operações vendidas não são realizadas porque o setup é puramente de alta, mas o sistema permite encerrar posições vendidas iniciadas por outros métodos.

Os stops são colocados a uma pequena distância abaixo do padrão para proteger contra sinais falsos, e as posições encerram se o preço fechar novamente abaixo desse nível.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
