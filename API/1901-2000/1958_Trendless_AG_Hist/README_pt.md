# Estratégia Trendless AG Histogram
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera reversões detectadas pelo indicador **Trendless AG Histogram**. O indicador mede a distância entre o preço e uma média móvel suavizada e suaviza o resultado novamente, formando um histograma ao redor de zero. Mínimos locais indicam possíveis reversões de alta enquanto máximos locais sugerem reversões de baixa.

As posições são abertas quando o histograma muda de direção. Se o indicador sobe após estar abaixo de valores anteriores, uma posição comprada é aberta. Se cai após estar acima de valores anteriores, uma posição vendida é aberta. Níveis opcionais de stop-loss e take-profit gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O valor do histograma está subindo enquanto o valor anterior era menor que seu predecessor.
  - **Vendido**: O valor do histograma está caindo enquanto o valor anterior era maior que seu predecessor.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal oposto ou níveis de stop-loss/take-profit.
- **Stops**: Stop-loss e take-profit fixos em unidades de preço.
- **Valores padrão**:
  - `Fast Length` = 7.
  - `Slow Length` = 5.
  - `Stop Loss` = 1000.
  - `Take Profit` = 2000.
  - `Candle Type` = velas de 12 horas.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Indicador personalizado baseado em médias móveis.
  - Stops: Sim.
  - Complexidade: Moderado.
  - Período: Médio prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Sim.
  - Nível de risco: Médio.
