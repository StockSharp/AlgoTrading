# Estratégia Parabolic RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que aplica Parabolic SAR ao RSI para detectar mudanças de tendência. A estratégia entra quando o SAR vira em relação à linha do RSI e pode filtrar operações por limiares de RSI.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `SAR` vira abaixo do RSI e (opcional) `RSI ≥ LongRsiMin`
  - Vendido: `SAR` vira acima do RSI e (opcional) `RSI ≤ ShortRsiMax`
- **Comprado/Vendido**: Configurável
- **Critérios de saída**: Virada oposta do SAR
- **Stops**: Nenhum
- **Valores padrão**:
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: Parabolic SAR, RSI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
