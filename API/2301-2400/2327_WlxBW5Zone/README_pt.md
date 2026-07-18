# Estratégia de Zona Wlx BW5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Awesome Oscillator (AO) e o Accelerator Oscillator (AC) de Bill Williams para identificar sequências de momentum forte. Um sinal de compra (venda) aparece quando ambos os osciladores sobem (descem) durante cinco barras consecutivas. O sistema reverte ou abre posições de acordo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `AO` e `AC` subindo durante cinco barras consecutivas.
  - **Vendido**: `AO` e `AC` descendo durante cinco barras consecutivas.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Reverter posição ao sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Timeframe` = 4 horas.
  - `Direct` = true.
  - `SignalBar` = 1.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
