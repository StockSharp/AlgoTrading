# Estratégia Xbug Free
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de média móvel contrária que compra quando o preço cruza abaixo de sua média móvel e vende quando o preço cruza acima. Usa distâncias simétricas de take-profit e stop-loss.

## Detalhes

- **Critérios de entrada**: preço cruzando abaixo/acima da média móvel simples
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto ou stop de proteção
- **Stops**: Sim
- **Valores padrão**:
  - `MaPeriod` = 19
  - `MaShift` = 15
  - `StopPoints` = 270
  - `Volume` = 0.1
  - `CandleType` = 4-hour
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
