# Estratégia de Full Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A configuração Full Candle entra quando uma vela fecha além de sua EMA e deixa apenas uma sombra pequena no lado do rompimento. A intenção é operar velas de momentum que mostrem ação decisiva sem muito rejeição. Saídas opcionais de take-profit e stop-loss baseadas em porcentagem gerenciam a operação assim que ela está aberta.

O sistema é mais adequado para rompimentos de curto prazo onde velas fortes frequentemente levam a um seguimento rápido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: vela altista fechando acima da EMA com sombra ≤ limiar
  - **Vendido**: vela baixista fechando abaixo da EMA com sombra ≤ limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Percentuais de take-profit ou stop-loss se habilitados
- **Stops**: Opcional
- **Valores padrão**:
  - `EmaLength` = 10
  - `ShadowPercent` = 5
  - `TPPercent` = 1.2
  - `SLPercent` = 1.8
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, price action
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
