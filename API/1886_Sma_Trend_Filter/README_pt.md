# Estratégia de Filtro de Tendência SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia multi-período que analisa a inclinação de cinco médias móveis simples (períodos 5, 8, 13, 21, 34) em três períodos (15m, 1h, 4h). Calcula pontuações de alta e de baixa para cada período e opera quando todos se alinham em uma direção.

## Detalhes

- **Critérios de entrada**:
  - Comprado: os três períodos mostram pelo menos 50% das SMAs subindo
  - Vendido: os três períodos mostram pelo menos 50% das SMAs caindo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto com base no nível de fechamento
- **Stops**: Não
- **Valores padrão**:
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Multi-período
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
