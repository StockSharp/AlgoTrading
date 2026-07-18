# Estratégia de Histograma XKRI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no Kairi Relative Index (KRI) suavizado por uma média móvel exponencial. O sistema busca mínimos e máximos locais do oscilador suavizado e entra em posições compradas ou vendidas quando um padrão de reversão aparece.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - Vendido: `Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **Comprado/Vendido**: Ambos
- **Stops**: Take profit e stop loss em pontos
- **Valores padrão**:
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Kairi, EMA
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
