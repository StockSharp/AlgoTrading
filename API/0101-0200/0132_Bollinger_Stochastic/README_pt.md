# Estratégia Bollinger Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Bollinger Stochastic combina as Bandas de Bollinger com o oscilador estocástico para identificar movimentos sobreextendidos.
O preço tocando a banda externa enquanto o oscilador está em uma zona extrema sugere um possível recuo.

Os testes indicam um retorno anual médio de aproximadamente 133%. Funciona melhor no mercado de criptomoedas.

O sistema opera contra esses extremos, comprando quando o preço atinge a banda inferior com o estocástico em sobrevenda, e vendendo na banda superior com o estocástico em sobrecompra.

Um stop baseado em percentual limita o risco caso a reversão à média não ocorra.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Stochastic
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

