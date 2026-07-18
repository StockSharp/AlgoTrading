# Estratégia de Rebote no VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Preço Médio Ponderado por Volume (VWAP) é um popular referencial intradiário. Quando o preço se desvia significativamente do VWAP e então imprime uma vela de volta em direção a ele, frequentemente se segue um breve movimento de reversão. Esta estratégia opera esses rebotes.

Os testes indicam um retorno anual médio de aproximadamente 130%. Funciona melhor no mercado de ações.

Para cada barra, o VWAP atual é calculado. Se uma vela de alta fechar abaixo do VWAP, o sistema vai comprado; se uma vela de baixa fechar acima do VWAP, vai vendido. Um percentual fixo de stop-loss gerencia o risco, e as posições são tipicamente mantidas apenas até que um sinal oposto se forme ou o stop seja atingido.

Como opera contra os extremos intradiários, o método funciona melhor em mercados de faixa do que em tendências fortes.

## Detalhes

- **Critérios de entrada**: Fechamento abaixo do VWAP com vela de alta ou acima do VWAP com vela de baixa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

