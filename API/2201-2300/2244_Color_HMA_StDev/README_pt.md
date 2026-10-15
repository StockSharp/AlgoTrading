# Estratégia Color HMA StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na Média Móvel Hull com um filtro dinâmico de desvio padrão.

O sistema observa o quanto o preço se desvia do HMA. Quando o fechamento ultrapassa a
média por um múltiplo escolhido do desvio padrão, a estratégia entra comprada, e vice-versa para posições vendidas.
Um multiplicador mais amplo define uma zona de saída para que as posições sejam fechadas apenas após um retorno significativo dentro da banda.

Esta abordagem tenta capturar explosões rápidas de momentum enquanto evita ruído. A Média Móvel Hull reage rapidamente
a mudanças de tendência, e o desvio padrão se adapta à volatilidade permitindo que os limiares se expandam durante mercados turbulentos. A estratégia opera em ambas as direções e não usa stops fixos, confiando em vez disso na
reversão à média do preço em direção ao HMA.

## Detalhes

- **Critérios de entrada**: Fechamento cruzando HMA ± K1 * StdDev.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Fechamento cruzando HMA ± K2 * StdDev na direção oposta.
- **Stops**: Sem stop-loss ou take-profit fixos.
- **Valores padrão**:
  - `HmaPeriod` = 13
  - `StdPeriod` = 9
  - `K1` = 1.5m
  - `K2` = 2.5m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência, Volatilidade
  - Direção: Ambos
  - Indicadores: HMA, Desvio padrão
  - Stops: Não
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
