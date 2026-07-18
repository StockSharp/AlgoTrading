# Estratégia de Bollinger Rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bollinger Rompimento busca capturar movimentos que ultrapassam as Bollinger Bands
e continuam nessa direção. Quando o preço fecha acima da banda superior ou abaixo
da banda inferior, a estratégia entra na direção do rompimento se as confirmações
opcionais apoiarem a operação.

Filtros de RSI, Aroon e média móvel podem ser habilitados para validar o impulso
e a tendência. Um stop-loss opcional ajuda a controlar o risco. As posições são
fechadas quando o preço atinge a banda oposta ou o stop é acionado.

Essa abordagem favorece mercados propensos a tendências fortes, onde as rupturas
de banda levam à continuação em vez de reversão à média.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento acima da banda superior e todos os filtros habilitados confirmam.
  - **Vendido**: Fechamento abaixo da banda inferior e todos os filtros habilitados confirmam.
- **Critérios de saída**: Toque da banda oposta ou stop-loss se `UseSL`.
- **Stops**: Stop-loss opcional (`UseSL`).
- **Valores padrão**:
  - `UseRSI` = True
  - `UseAroon` = False
  - `UseMA` = True
  - `UseSL` = True
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado/Vendido
  - Indicadores: Bollinger Bands, RSI, Aroon, Moving Average
  - Complexidade: Moderado
  - Nível de risco: Alto
