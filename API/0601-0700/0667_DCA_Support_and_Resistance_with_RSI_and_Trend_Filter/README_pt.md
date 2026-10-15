# Estratégia DCA de Suporte e Resistência com RSI e Filtro de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de custo médio em dólares usando níveis de suporte/resistência, RSI e filtro de tendência EMA. Compra no suporte em uma tendência de alta quando o RSI está sobrevendido e vende na resistência em uma tendência de baixa quando o RSI está sobrecomprado.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço no suporte, RSI abaixo de sobrevendido, acima do EMA
  - Vendido: preço na resistência, RSI acima de sobrecomprado, abaixo do EMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: preço atinge a resistência ou RSI acima de sobrecomprado
  - Vendido: preço atinge o suporte ou RSI abaixo de sobrevendido
- **Stops**: Nenhum
- **Valores padrão**:
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, EMA, Highest, Lowest
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
