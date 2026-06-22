# Delta WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Delta WPR compara um oscilador Williams %R rápido e um lento para capturar mudanças de momentum. Quando o valor rápido supera o lento e o oscilador lento permanece acima de um nível limiar, a estratégia abre uma posição comprada e fecha qualquer exposição vendida. A configuração oposta — rápido abaixo do lento com o oscilador lento abaixo do nível — aciona uma entrada vendida. Cada nova vela é processada apenas após a conclusão para evitar ruído.

Backtests com dados de 4 horas mostram que a abordagem tem melhor desempenho em mercados laterais onde o Williams %R oscila entre zonas de sobrecompra e sobrevenda.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `WPR slow > Level && WPR fast > WPR slow`
  - Vendido: `WPR slow < Level && WPR fast < WPR slow`
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 30
  - `Level` = -50m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: WilliamsR
  - Stops: Não
  - Complexidade: Básico
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
