# Estratégia Universal Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este modelo universal converte velas padrão em velas Heikin Ashi e opera na direção de seu corpo. O método suaviza o ruído do preço, permitindo que as tendências apareçam com mais clareza. É leve e pode servir como base para filtros ou saídas personalizadas.

O sistema entra comprado quando o fechamento Heikin Ashi está acima de sua abertura e inverte para vendido quando o fechamento cai abaixo da abertura.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `HA_Close > HA_Open`
  - **Vendido**: `HA_Close < HA_Open`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
