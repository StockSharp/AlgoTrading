# Estratégia FatlSatlOsma
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo reproduz a lógica do especialista MetaTrader **Exp_FatlSatlOsma** usando a API de alto nível do StockSharp.  
O sistema original trabalha com o oscilador Fatl/Satl (um indicador personalizado semelhante ao MACD).  
A estratégia busca uma mudança na direção do oscilador:

- Quando o oscilador sobe por duas barras e o último valor é maior que o anterior, uma posição comprada é aberta e as posições vendidas são fechadas.
- Quando o oscilador cai por duas barras e o último valor é menor que o anterior, uma posição vendida é aberta e as posições compradas são fechadas.

O oscilador é implementado através do indicador integrado `MovingAverageConvergenceDivergenceSignal` com períodos rápidos e lentos configuráveis.  
Os valores padrão correspondem aos parâmetros originais do FATL/SATL.

## Detalhes

- **Critérios de entrada**: aceleração do oscilador.
- **Comprado/Vendido**: ambos.
- **Critérios de saída**: aceleração oposta.
- **Stops**: nenhum.
- **Valores padrão**:
  - `Fast` = 39
  - `Slow` = 65
  - `CandleType` = período de 12 horas
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
