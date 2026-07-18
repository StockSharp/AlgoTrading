# Estratégia 3Commas Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Versão simplificada da estratégia 3Commas Bot. Opera quando uma EMA rápida cruza uma EMA mais lenta e gerencia o risco usando um stop baseado em ATR. Um alvo de recompensa fixo e um stop trailing de ATR opcional são suportados.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop de ATR, take profit opcional, stop trailing de ATR opcional após atingir o limiar de recompensa.
- **Stops**: Baseados em ATR.
- **Valores padrão**:
  - `MaLength1` = 21
  - `MaLength2` = 50
  - `AtrLength` = 14
  - `RnR` = 1
  - `RiskM` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
