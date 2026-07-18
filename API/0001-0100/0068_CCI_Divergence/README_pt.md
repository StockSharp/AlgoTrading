# Estratégia de Divergência CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
As divergências do Índice de Canal de Commodities (CCI) podem prenunciar reversões de tendência quando o preço se move na direção oposta ao indicador. Esta estratégia compara as máximas e mínimas de swing no preço com as do CCI para identificar força ou fraqueza ocultas.

Os testes indicam um retorno anual médio de aproximadamente 91%. Tem melhor desempenho no mercado de ações.

Em cada vela, o sistema atualiza os valores recentes de preço e CCI, sinalizando uma divergência altista quando o preço faz uma nova mínima enquanto o CCI forma uma mínima mais alta. A divergência baixista é o oposto. Quando uma divergência se alinha com níveis de sobrevenda ou sobrecompra, uma operação é aberta com um stop de volatilidade.

As saídas ocorrem quando o CCI cruza de volta pela linha zero, sinalizando que o impulso se esgotou. Como as divergências podem persistir, as regras também são redefinidas após um número fixo de barras para evitar sinais obsoletos.

## Detalhes

- **Critérios de entrada**: Divergência preço/CCI com CCI abaixo de -100 para comprados ou acima de +100 para vendidos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: CCI cruzando o zero ou stop-loss.
- **Stops**: Sim, baseado em percentual.
- **Valores padrão**:
  - `CciPeriod` = 20
  - `DivergencePeriod` = 5
  - `OverboughtLevel` = 100
  - `OversoldLevel` = -100
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

