# Estratégias Paralelas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento Heikin Ashi com MACD que opera em ambas as direções. Entra quando uma nova tendência Heikin Ashi se alinha com um rompimento acima ou abaixo do Canal Donchian e o MACD confirma o momentum.

Combinar a identificação de tendência do Heikin Ashi com a detecção de rompimentos mantém as operações alinhadas com movimentos frescos. O MACD atua como filtro de momentum para evitar sinais falsos.

Melhor para traders que buscam entradas antecipadas de rompimento após uma reversão de tendência. Funciona em períodos intradiários.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Trend turns bullish && Close > DonchianHigh && MACD > Signal`
  - Vendido: `Trend turns bearish && Close < DonchianLow && MACD < Signal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal de rompimento oposto
- **Stops**: Não definidos
- **Valores padrão**:
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Heikin Ashi, Donchian Channel, MACD
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
