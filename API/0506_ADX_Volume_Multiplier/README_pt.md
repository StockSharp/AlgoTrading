# Estratégia de Multiplicador de Volume ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Multiplicador de Volume ADX combina a força de tendência do Average Directional Index com um filtro de aumento de volume. Entra em operações apenas quando o ADX supera um limiar, a linha direcional dominante aponta para a direção da tendência e o volume atual supera uma média móvel multiplicada por um fator definido pelo usuário.

## Detalhes

- **Critérios de entrada**:
  - ADX acima do limiar e DI+ > DI- com volume maior que SMA(volume) * multiplicador → comprado.
  - ADX acima do limiar e DI- > DI+ com volume maior que SMA(volume) * multiplicador → vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Um sinal reverso aciona a reversão de posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `AdxPeriod` = 21
  - `AdxThreshold` = 26
  - `VolumeMultiplier` = 1.8
  - `VolumePeriod` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, Volume SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
