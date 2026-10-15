# Estratégia de Rompimento de Triângulo para BTC (MARK804)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera rompimentos do triângulo de média móvel simples quando o volume dispara e gerencia posições com stops baseados em ATR.

## Detalhes

- **Critérios de entrada**: preço cruzando acima da linha SMA superior ou abaixo da linha SMA inferior com volume acima de sua SMA
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou take-profit baseados em ATR
- **Stops**: Sim
- **Valores padrão**:
  - `TriangleLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrLength` = 14
  - `VolumeMultiplier` = 1.5
  - `AtrMultiplierSl` = 1.0
  - `AtrMultiplierTp` = 1.5
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: SMA, ATR, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
