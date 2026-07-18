# Estratégia CC Trend 2 Baixista Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente vendida que vende quando o fechamento anterior está abaixo da máxima dinâmica de Fibonacci e EMA21 está abaixo de EMA55. Sai quando o preço cruza acima de EMA200 com lucro não negativo ou quando o fechamento anterior sobe acima do nível Fibonacci 0.236 e nenhum novo sinal vendido aparece.

## Detalhes

- **Critérios de entrada**:
  - Vendido: fechamento anterior abaixo da máxima de Fibonacci e EMA21 abaixo de EMA55
- **Comprado/Vendido**: Vendido
- **Critérios de saída**:
  - Preço cruza acima de EMA200 com lucro
  - Fechamento anterior acima do nível Fibonacci 0.236 sem novo sinal vendido
- **Stops**: Nenhum
- **Valores padrão**:
  - `FibLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Vendido
  - Indicadores: EMA, Fibonacci
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
