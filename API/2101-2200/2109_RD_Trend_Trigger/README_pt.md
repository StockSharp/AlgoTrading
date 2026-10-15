# Estratégia RD Trend Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia RD Trend Trigger usa o oscilador RD-TrendTrigger para capturar reversões de tendência ou rompimentos de níveis dependendo do modo selecionado. No modo twist, as operações seguem as mudanças de direção do oscilador; no modo disposition, as operações ocorrem quando o oscilador cruza níveis predefinidos.

## Detalhes

- **Critérios de entrada**:
  - **Modo twist**: Entrar comprado quando o oscilador vira para cima; entrar vendido quando vira para baixo.
  - **Modo disposition**: Entrar comprado quando o oscilador sobe acima de `HighLevel`; entrar vendido quando cai abaixo de `LowLevel`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinais opostos ou condições de saída explícitas no modo disposition quando o oscilador sobe acima de `LowLevel`.
- **Stops**: Nenhum por padrão; a proteção pode ser ativada externamente.
- **Valores padrão**:
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado & Vendido
  - Indicadores: RD-TrendTrigger personalizado (baseado em máximas/mínimas e Tillson T3)
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
