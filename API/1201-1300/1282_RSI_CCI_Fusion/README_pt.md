# Estratégia de Fusão RSI-CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina RSI e CCI padronizados em um único oscilador com bandas dinâmicas.
Compra quando o valor fusionado cruza acima da banda inferior e vende ou fica vendido quando cruza abaixo da banda superior.

## Detalhes

- **Critérios de entrada**: fusão reescalada cruza acima da banda inferior para comprado; cruza abaixo da banda superior para vendido
- **Comprado/Vendido**: Ambos (vendido opcional)
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 14
  - `RsiWeight` = 0.5
  - `EnableShort` = false
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, CCI, SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

