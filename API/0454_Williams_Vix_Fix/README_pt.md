# Estratégia Williams VIX Fix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Williams VIX Fix adapta o indicador de volatilidade de Larry Williams para
instrumentos que não possuem um VIX publicado. Ela calcula um valor VIX sintético usando
a distância entre o fechamento mais alto durante um período de referência e a mínima
atual. Quando esse valor sobe acima de um limiar de Bollinger Band ou o preço fecha
abaixo da banda inferior de Bollinger, a estratégia considera uma oportunidade de
sobrevenda. Um cálculo invertido mede os extremos de sobrecompra.

A abordagem busca reversão à média após picos de volatilidade. Quando o VIX Fix sinaliza
alto medo e o preço está abaixo da banda inferior, uma operação comprada é aberta. Por
outro lado, quando o VIX Fix inverso aponta para complacência extrema e o preço está
acima da banda superior, as posições compradas existentes são fechadas. Limites de
percentil controlam a sensibilidade.

## Detalhes

- **Critérios de entrada**:
  - VIX Fix ≥ banda superior ou percentil e preço < banda inferior de Bollinger.
- **Comprado/Vendido**: Entradas compradas com saídas em sinal oposto.
- **Critérios de saída**:
  - VIX Fix invertido ≥ banda superior ou percentil e preço > banda superior de Bollinger.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **Filtros**:
  - Categoria: Reversão à média de volatilidade
  - Direção: Comprado
  - Indicadores: Bollinger Bands, Williams VIX Fix
  - Stops: Não
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
