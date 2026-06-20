# CCI Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia CCI Hook Reversal usa o Commodity Channel Index como gatilho quando ele engancha para longe de uma leitura extrema. Após o indicador ultrapassar +100 ou cair abaixo de -100, ele frequentemente recua rapidamente à medida que o momentum para.

Os testes indicam um retorno anual médio de aproximadamente 169%. Funciona melhor no mercado de criptomoedas.

Operações compradas ocorrem quando o CCI vira para cima a partir da sobrevenda enquanto o preço ainda imprime uma nova mínima marginal. As operações vendidas são iniciadas quando o CCI reverte a partir da sobrecompra com o preço atingindo novas máximas.

Cada operação carrega um pequeno stop fixo e é encerrada quando o CCI engancha de volta na direção oposta ou o stop é atingido.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
