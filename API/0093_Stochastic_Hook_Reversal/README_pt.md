# Stochastic Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Stochastic Hook Reversal observa a linha %K em busca de um gancho saindo do território de sobrecompra ou sobrevenda. Após se estender a um extremo, o oscilador frequentemente se curva de volta, indicando que o momentum está diminuindo.

Os testes indicam um retorno anual médio de aproximadamente 166%. Funciona melhor no mercado de ações.

O sistema entra comprado quando %K vira para cima a partir de abaixo de vinte enquanto o preço pressiona uma nova mínima. Vende a descoberto quando o oscilador engancha para baixo a partir de acima de oitenta durante um empurrão final para cima.

As posições usam um pequeno stop percentual e fecham quando o estocástico engata na outra direção ou o stop é atingido.

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
  - Indicadores: Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
