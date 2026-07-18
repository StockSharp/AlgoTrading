# Estratégia Double Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Double Supertrend emprega duas médias móveis baseadas em ATR com diferentes períodos
e multiplicadores. A primeira linha define a direção da operação, enquanto a segunda
pode atuar como alvo ou saída trailing. Essa combinação permite um seguimento de
tendência flexível com parâmetros de lucro e risco definidos.

Quando o preço se move acima de ambas as linhas e a estratégia está configurada para
operar comprado, uma posição é aberta. Para operações vendidas, as condições são
espelhadas. As saídas dependem do tipo de tomada de lucro selecionado ou de um
stop-loss percentual.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**: O preço cruza as linhas de Supertrend na `Direction` permitida.
- **Critérios de saída**: Quebra da linha oposta, tomada de lucro (`TPType`/`TPPercent`) ou stop-loss (`SLPercent`).
- **Stops**: Stop percentual baseado em `SLPercent`.
- **Valores padrão**:
  - `ATRPeriod1` = 10
  - `Factor1` = 3.0
  - `ATRPeriod2` = 20
  - `Factor2` = 5.0
  - `Direction` = "Long"
  - `TPType` = "Supertrend"
  - `TPPercent` = 1.5
  - `SLPercent` = 10.0
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Configurável
  - Indicadores: ATR‑based Supertrend
  - Complexidade: Avançado
  - Nível de risco: Médio
