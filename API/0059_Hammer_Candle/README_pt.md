# Reversão por Vela Martelo (Hammer Candle Reversal)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

As velas martelo (Hammer) frequentemente marcam uma reversão intradiária após a pressão vendedora diminuir. Esta estratégia busca o padrão martelo e entra comprado, antecipando uma recuperação.

Os testes indicam um retorno anual médio de aproximadamente 64%. Funciona melhor no mercado forex.

O sistema requer uma sombra inferior de pelo menos o dobro do corpo e pouca sombra superior. Uma vez identificado, compra com o tamanho de posição definido e aguarda o lucro ou o stop-loss.

## Detalhes

- **Critérios de entrada**: Vela martelo detectada.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Stop-loss ou saída discricionária.
- **Stops**: Sim.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Somente comprado
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
