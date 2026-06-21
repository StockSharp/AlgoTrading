# Estratégia de Reversão VRS Vegas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de reversão que utiliza as mechas dos candles.

Os testes indicam um retorno anual médio de aproximadamente 37%. Funciona melhor no mercado de criptomoedas.

O sistema procura grandes picos em relação ao preço de fechamento. Uma mecha inferior grande aciona uma entrada comprada, enquanto uma mecha superior grande aciona uma entrada vendida. As posições são fechadas quando o preço avança o dobro do tamanho do pico em lucro.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: mecha inferior ≥ Spike% * close e sem pico superior.
  - **Vendido**: mecha superior ≥ Spike% * close e sem pico inferior.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Alvo na entrada ± (pico * 2).
- **Stops**: Não.
- **Valores padrão**:
  - `SpikePercent` = 0.025
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Price action
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
